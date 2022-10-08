module Exercism.TestRunner.FSharp.Rewrite

open System.IO
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Visitor
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text
open FSharp.Compiler.Xml
open Fantomas.Core
open Fantomas.FCS.Parse

type ParseResult =
    | ParseSuccess of Code: ISourceText * Tree: ParsedInput
    | ParseError

type RewriteResult =
    | RewriteSuccess of OriginalCode: ISourceText * OriginalTestTree: ParsedInput * RewrittenCode: ISourceText * OriginalProjectFile: string * RewrittenProjectFile: string
    | RewriteError

type EnableAllTests() =
    inherit SyntaxVisitor()
    
    default this.VisitSynAttributeList(attrs: SynAttributeList) : SynAttributeList =
        base.VisitSynAttributeList(
            { attrs with
                Attributes =
                  attrs.Attributes
                  |> List.filter (fun attr -> attr.TypeName.LongIdent.Head.idText <> "Ignore") })

    override _.VisitSynAttribute(attr: SynAttribute) : SynAttribute =
        let isSkipExpr expr =
            match expr with
            | SynExpr.App(_, _, SynExpr.App(_, _, _, SynExpr.Ident(ident), _), _, _) -> ident.idText = "Skip"
            | _ -> false
        
        match attr.ArgExpr with
        | SynExpr.Paren(expr, leftParenRange, rightParenRange, range) ->
            match expr with
            | SynExpr.App _ when isSkipExpr expr ->
                let newExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range) 
                base.VisitSynAttribute({ attr with ArgExpr = newExpr })
            | SynExpr.Tuple(iStruct, exprs, commaRanges, tplRange) ->
                let newExpr =
                    SynExpr.Paren(
                        SynExpr.Tuple(iStruct, exprs |> List.filter (isSkipExpr >> not), commaRanges, tplRange), leftParenRange, rightParenRange, range)                
                base.VisitSynAttribute({ attr with ArgExpr = newExpr })
            | _ -> base.VisitSynAttribute(attr)
        | _ -> base.VisitSynAttribute(attr)

let private parseFile (filePath: string) =
    if File.Exists(filePath) then
        let source = File.ReadAllText(filePath) |> SourceText.ofString
        let tree, _diagnostics = parseFile false source []
        Some tree // TODO: use diagnostics to determine success
        |> Option.map (fun tree -> ParseSuccess(source, tree))
        |> Option.defaultValue ParseError
    else
        ParseError

let private toCode tree =
    CodeFormatter.FormatASTAsync(tree, "", FormatConfig.FormatConfig.Default)
    |> Async.RunSynchronously
    |> SourceText.ofString

let private enableAllTests parsedInput =
    EnableAllTests().VisitInput(parsedInput)
    
let private rewriteProjectFile (context: TestRunContext) =
    let originalProjectFile = File.ReadAllText(context.ProjectFile)
    originalProjectFile, originalProjectFile.Replace("net5.0", "net6.0")

let rewriteTests (context: TestRunContext) =
    match parseFile context.TestsFile with
    | ParseSuccess (originalTestCode, originalTestTree) ->
        let rewrittenTestCode = originalTestTree |> enableAllTests |> toCode
        let (originalProjectFile, rewrittenProjectFile) = rewriteProjectFile context
        RewriteSuccess(originalTestCode, originalTestTree, rewrittenTestCode, originalProjectFile, rewrittenProjectFile)
    | ParseError -> RewriteError
