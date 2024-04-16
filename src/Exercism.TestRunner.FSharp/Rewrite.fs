module Exercism.TestRunner.FSharp.Rewrite

open System.IO
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Visitor
open Fantomas.Core
open Fantomas.FCS.Syntax
open Fantomas.FCS.Text
open Fantomas.FCS.Parse

type ParseResult =
    | ParseSuccess of Source: string * SourceText: ISourceText * Tree: ParsedInput
    | ParseError

type RewriteResult =
    | RewriteSuccess of OriginalCode: ISourceText * OriginalTestTree: ParsedInput * RewrittenCode: ISourceText * OriginalProjectFile: string * RewrittenProjectFile: string
    | RewriteError

type EnableAllTests() =
    inherit SyntaxVisitor()

    override _.VisitSynAttribute(attr: SynAttribute) : SynAttribute =
        let isSkipExpr expr =
            match expr with
            | SynExpr.App(_, _, SynExpr.App(_, _, _, SynExpr.Ident(ident), _), _, _) -> ident.idText = "Skip"
            | _ -> false
        
        match attr.ArgExpr with
        | SynExpr.Paren(expr, leftParenRange, rightParenRange, range) ->            
            match expr with
            | SynExpr.App(flag, isInfix, funcExpr, argExpr, range) ->
                match funcExpr with
                | SynExpr.App(_, _, _, SynExpr.Ident(ident), _) ->
                    if ident.idText = "Skip" then
                        let noAttributesArgExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range)
                        base.VisitSynAttribute({ attr with ArgExpr = noAttributesArgExpr })
                    else
                        base.VisitSynAttribute(attr)
                | _ -> base.VisitSynAttribute(attr)                
            | _ -> base.VisitSynAttribute(attr)
            // () _ when isSkipExpr expr ->
            //     let newExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range) 
            //     base.VisitSynAttribute({ attr with ArgExpr = newExpr })
            // | SynExpr.Tuple(iStruct, exprs, commaRanges, tplRange) ->
            //     let newExpr =
            //         SynExpr.Paren(
            //             SynExpr.Tuple(iStruct, exprs |> List.filter (isSkipExpr >> not), commaRanges, tplRange), leftParenRange, rightParenRange, range)                
            //     base.VisitSynAttribute({ attr with ArgExpr = newExpr })
            // | _ -> base.VisitSynAttribute(attr)
        | _ -> base.VisitSynAttribute(attr)

let private parseFile (filePath: string) =
    if File.Exists(filePath) then
        let source = File.ReadAllText(filePath)
        let sourceText = source |> SourceText.ofString        
        let tree, _ = CodeFormatter.ParseAsync(false, source) |> Async.RunSynchronously |> Array.head
        Some tree
        |> Option.map (fun tree -> ParseSuccess(source, sourceText, tree))
        |> Option.defaultValue ParseError
    else
        ParseError

let private toCode tree =
    CodeFormatter.FormatASTAsync(tree)
    |> Async.RunSynchronously
    |> SourceText.ofString

let private enableAllTests parsedInput =
    parsedInput
    // EnableAllTests().VisitInput(parsedInput)
    
let private rewriteProjectFile (context: TestRunContext) =
    let originalProjectFile = File.ReadAllText(context.ProjectFile)
    let rewrittenProjectFile =
        originalProjectFile
            .Replace("net5.0", "net8.0")
            .Replace("net6.0", "net8.0")
            .Replace("net7.0", "net8.0")
    originalProjectFile, rewrittenProjectFile        

let rewriteTests (context: TestRunContext) =
    match parseFile context.TestsFile with
    | ParseSuccess (originalSource, originalSourceText, originalTestTree) ->        
        let rewrittenTestCode = originalTestTree |> enableAllTests |> toCode
        let originalProjectFile, rewrittenProjectFile = rewriteProjectFile context
        RewriteSuccess(originalSourceText, originalTestTree, rewrittenTestCode, originalProjectFile, rewrittenProjectFile)
    | ParseError -> RewriteError
