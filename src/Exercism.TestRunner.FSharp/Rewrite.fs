module Exercism.TestRunner.FSharp.Rewrite

open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Visitor
open System.IO
open FSharp.Compiler.Text
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.XmlDoc
open Fantomas

type ParseResult =
    | ParseSuccess of Code: ISourceText * Tree: ParsedInput
    | ParseError

type RewriteResult =
    | RewriteSuccess of OriginalCode: ISourceText * OriginalTestTree: ParsedInput * RewrittenCode: ISourceText
    | RewriteError

type EnableAllTests() =
    inherit SyntaxVisitor()

    override _.VisitSynAttribute(attr: SynAttribute) : SynAttribute =
        match attr.ArgExpr with
        | SynExpr.Paren(
            SynExpr.App(_, _,
                SynExpr.App(_, _, _, SynExpr.Ident(ident), _), _, _), _, _, _) when ident.idText = "Skip" ->
                base.VisitSynAttribute({ attr with ArgExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range)})
            | _ -> base.VisitSynAttribute(attr)
        | _ -> base.VisitSynAttribute(attr)

type CaptureConsoleOutput() =
    inherit SyntaxVisitor()

    override this.VisitSynModuleOrNamespace(modOrNs: SynModuleOrNamespace) : SynModuleOrNamespace =
        match modOrNs with
        | SynModuleOrNamespace (longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            let letDecls, otherDecls =
                decls
                |> List.partition
                    (function
                    | SynModuleDecl.Let (_, _, _) -> true
                    | _ -> false)

            let letDeclBindings =
                letDecls
                |> List.choose
                    (function
                    | SynModuleDecl.Let (isRecursive, bindings, _) ->
                        Some(SynMemberDefn.LetBindings(bindings, false, isRecursive, range))
                    | _ -> None)

            let ident name = Ident(name, range)

            let longIdentWithDots names =
                LongIdentWithDots.LongIdentWithDots(names |> List.map (fun name -> ident name), [])

            let constructor =
                SynMemberDefn.ImplicitCtor(
                    None,
                    [],
                    SynSimplePats.SimplePats(
                        [ SynSimplePat.Typed(
                              SynSimplePat.Id(ident "testOutput", None, false, false, false, range),
                              SynType.LongIdent(
                                  longIdentWithDots [ "Xunit"
                                                      "Abstractions"
                                                      "ITestOutputHelper" ]
                              ),
                              range
                          ) ],
                        range
                    ),
                    None,
                    PreXmlDoc.Empty,
                    range
                )

            let stringWriter =
                SynMemberDefn.LetBindings(
                    [ SynBinding.Binding(
                          None,
                          SynBindingKind.NormalBinding,
                          false,
                          false,
                          [],
                          PreXmlDoc.Empty,
                          SynValData(None, SynValInfo([], SynArgInfo([], false, None)), None),
                          SynPat.Named(SynPat.Wild(range), ident "stringWriter", false, None, range),
                          None,
                          SynExpr.New(
                              false,
                              SynType.LongIdent(
                                  longIdentWithDots [ "System"
                                                      "IO"
                                                      "StringWriter" ]
                              ),
                              SynExpr.Const(SynConst.Unit, range),
                              range
                          ),
                          range,
                          NoDebugPointAtLetBinding
                      ) ],
                    false,
                    false,
                    range
                )

            let captureOutput =
                SynMemberDefn.LetBindings(
                    [ Binding(
                          None,
                          DoBinding,
                          false,
                          false,
                          [],
                          PreXmlDoc.Empty,
                          SynValData(None, SynValInfo([], SynArgInfo([], false, None)), None),
                          SynPat.Const(SynConst.Unit, range),
                          None,
                          SynExpr.Sequential(
                              DebugPointAtSequential.Both,
                              true,
                              SynExpr.App(
                                  ExprAtomicFlag.Atomic,
                                  false,
                                  SynExpr.LongIdent(
                                      false,
                                      longIdentWithDots [ "System"
                                                          "Console"
                                                          "SetOut" ],
                                      None,
                                      range
                                  ),
                                  SynExpr.Paren(SynExpr.Ident(ident "stringWriter"), range, None, range),
                                  range
                              ),
                              SynExpr.Sequential(
                                  DebugPointAtSequential.Both,
                                  true,
                                  SynExpr.App(
                                      ExprAtomicFlag.Atomic,
                                      false,
                                      SynExpr.LongIdent(
                                          false,
                                          longIdentWithDots [ "System"
                                                              "Console"
                                                              "SetError" ],
                                          None,
                                          range
                                      ),
                                      SynExpr.Paren(SynExpr.Ident(ident "stringWriter"), range, None, range),
                                      range
                                  ),
                                  SynExpr.App(
                                      ExprAtomicFlag.NonAtomic,
                                      false,
                                      SynExpr.App(
                                          ExprAtomicFlag.NonAtomic,
                                          true,
                                          SynExpr.Ident(ident "op_PipeRight"),
                                          SynExpr.App(
                                              ExprAtomicFlag.Atomic,
                                              false,
                                              SynExpr.LongIdent(
                                                  false,
                                                  longIdentWithDots [ "System"
                                                                      "Diagnostics"
                                                                      "Trace"
                                                                      "Listeners"
                                                                      "Add" ],
                                                  None,
                                                  range
                                              ),
                                              SynExpr.Paren(
                                                  SynExpr.New(
                                                      false,
                                                      SynType.LongIdent(
                                                          longIdentWithDots [ "System"
                                                                              "Diagnostics"
                                                                              "ConsoleTraceListener" ]
                                                      ),
                                                      SynExpr.Const(SynConst.Unit, range),
                                                      range
                                                  ),
                                                  range,
                                                  None,
                                                  range
                                              ),
                                              range
                                          ),
                                          range
                                      ),
                                      SynExpr.Ident(ident "ignore"),
                                      range
                                  ),
                                  range
                              ),
                              range
                          ),
                          range,
                          NoDebugPointAtDoBinding
                      ) ],
                    false,
                    false,
                    range
                )

            let interfaceImpl =
                SynMemberDefn.Interface(
                    SynType.LongIdent(
                        longIdentWithDots [ "System"
                                            "IDisposable" ]
                    ),
                    Some [ SynMemberDefn.Member(
                               Binding(
                                   None,
                                   NormalBinding,
                                   false,
                                   false,
                                   [],
                                   PreXmlDoc.Empty,
                                   SynValData(
                                       Some
                                           { IsInstance = true
                                             IsDispatchSlot = false
                                             IsOverrideOrExplicitImpl = true
                                             IsFinal = false
                                             MemberKind = MemberKind.Member },
                                       SynValInfo([], SynArgInfo([], false, None)),
                                       None
                                   ),
                                   SynPat.LongIdent(
                                       longIdentWithDots [ "__"; "Dispose" ],
                                       None,
                                       None,
                                       Pats([ SynPat.Paren(SynPat.Const(SynConst.Unit, range), range) ]),
                                       None,
                                       range
                                   ),
                                   None,
                                   SynExpr.App(
                                       ExprAtomicFlag.Atomic,
                                       false,
                                       SynExpr.LongIdent(
                                           false,
                                           longIdentWithDots [ "testOutput"
                                                               "WriteLine" ],
                                           None,
                                           range
                                       ),
                                       SynExpr.Paren(
                                           SynExpr.App(
                                               ExprAtomicFlag.Atomic,
                                               false,
                                               SynExpr.LongIdent(
                                                   false,
                                                   longIdentWithDots [ "stringWriter"
                                                                       "ToString" ],
                                                   None,
                                                   range
                                               ),
                                               SynExpr.Const(SynConst.Unit, range),
                                               range
                                           ),
                                           range,
                                           None,
                                           range

                                       ),
                                       range
                                   ),
                                   range,
                                   NoDebugPointAtInvisibleBinding
                               ),
                               range
                           ) ],
                    range
                )

            let newDecls =
                otherDecls
                @ [ SynModuleDecl.Types(
                        [ TypeDefn(
                              ComponentInfo([], [], [], [ ident "Tests" ], doc, false, None, range),
                              SynTypeDefnRepr.ObjectModel(
                                  TyconClass,
                                  letDeclBindings
                                  @ [ constructor
                                      stringWriter
                                      captureOutput
                                      interfaceImpl ],
                                  range
                              ),
                              [],
                              range
                          ) ],
                        range
                    ) ]

            SynModuleOrNamespace(
                this.VisitLongIdent longIdent,
                isRecursive,
                isModule,
                newDecls |> List.map this.VisitSynModuleDecl,
                doc,
                attrs |> List.map this.VisitSynAttributeList,
                Option.map this.VisitSynAccess access,
                range
            )

let private checker = FSharpChecker.Create()

let private parseTree (sourceText: ISourceText) (filePath: string) =
    let parseOptions =
        { FSharpParsingOptions.Default with
              SourceFiles = [| filePath |] }

    let parseResult =
        checker.ParseFile(filePath, sourceText, parseOptions)
        |> Async.RunSynchronously

    parseResult.ParseTree

let private parseFile (filePath: string) =
    if File.Exists(filePath) then
        let sourceText =
            File.ReadAllText(filePath) |> SourceText.ofString

        parseTree sourceText filePath
        |> Option.map (fun tree -> ParseSuccess(sourceText, tree))
        |> Option.defaultValue ParseError
    else
        ParseError

let private toCode tree =
    CodeFormatter.FormatASTAsync(tree, "", [], None, FormatConfig.FormatConfig.Default)
    |> Async.RunSynchronously
    |> SourceText.ofString

let private enableAllTests parsedInput =
    let visitors : SyntaxVisitor list =
        [ EnableAllTests()
          CaptureConsoleOutput() ]

    visitors
    |> List.fold (fun tree visitor -> visitor.VisitInput(tree)) parsedInput

let rewriteTests (context: TestRunContext) =
    match parseFile context.TestsFile with
    | ParseSuccess (originalTestCode, originalTestTree) ->
        let rewrittenTestCode =
            originalTestTree |> enableAllTests |> toCode

        RewriteSuccess(originalTestCode, originalTestTree, rewrittenTestCode)
    | ParseError -> RewriteError
