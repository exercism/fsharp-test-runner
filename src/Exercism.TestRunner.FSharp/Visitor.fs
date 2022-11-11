module Exercism.TestRunner.FSharp.Visitor

open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text
open FSharp.Compiler.Xml

type SyntaxVisitor() =
    abstract VisitInput: ParsedInput -> ParsedInput

    default this.VisitInput(input: ParsedInput): ParsedInput =
        match input with
        | ParsedInput.ImplFile(ParsedImplFileInput(file, isScript, qualName, pragmas, hashDirectives, modules, isLastCompiland, trivia)) ->
            ParsedInput.ImplFile
                (ParsedImplFileInput
                    (file, isScript, qualName, pragmas, hashDirectives,
                     List.map this.VisitSynModuleOrNamespace modules, isLastCompiland, this.VisitParsedImplFileInputTrivia(trivia)))
        | ParsedInput.SigFile(ParsedSigFileInput(file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs, trivia)) ->
            ParsedInput.SigFile(ParsedSigFileInput(file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs, trivia))

    abstract VisitParsedImplFileInputTrivia : ParsedImplFileInputTrivia -> ParsedImplFileInputTrivia    
    default this.VisitParsedImplFileInputTrivia(trivia: ParsedImplFileInputTrivia) = trivia
    
    abstract VisitSynModuleOrNamespace: SynModuleOrNamespace -> SynModuleOrNamespace

    default this.VisitSynModuleOrNamespace(modOrNs: SynModuleOrNamespace): SynModuleOrNamespace =
        match modOrNs with
        | SynModuleOrNamespace(longIdent, isRecursive, isModule, decls, doc, attrs, access, range, trivia) ->
            SynModuleOrNamespace
                (this.VisitLongIdent longIdent, isRecursive, isModule, decls |> List.map this.VisitSynModuleDecl, this.VisitPreXmlDoc(doc),
                 attrs |> List.map this.VisitSynAttributeList, Option.map this.VisitSynAccess access, range, trivia)

    abstract VisitSynModuleDecl: SynModuleDecl -> SynModuleDecl

    default this.VisitSynModuleDecl(synMod: SynModuleDecl): SynModuleDecl =
        match synMod with
        | SynModuleDecl.ModuleAbbrev(ident, longIdent, range) ->
            SynModuleDecl.ModuleAbbrev(this.VisitIdent ident, this.VisitLongIdent longIdent, range)
        | SynModuleDecl.NestedModule(sci, isRecursive, decls, isContinuing, range, trivia) ->
            SynModuleDecl.NestedModule
                (this.VisitSynComponentInfo sci, isRecursive, decls |> List.map this.VisitSynModuleDecl, isContinuing, range, trivia)
        | SynModuleDecl.Let(isRecursive, bindings, range) ->
            SynModuleDecl.Let(isRecursive, bindings |> List.map this.VisitSynBinding, range)
        | SynModuleDecl.Expr(expr, range) -> SynModuleDecl.Expr(this.VisitSynExpr expr, range)
        | SynModuleDecl.Types(typeDefs, range) ->
            SynModuleDecl.Types(typeDefs |> List.map this.VisitSynTypeDefn, range)
        | SynModuleDecl.Exception(exceptionDef, range) ->
            SynModuleDecl.Exception(this.VisitSynExceptionDefn exceptionDef, range)
        | SynModuleDecl.Open(target, range) -> SynModuleDecl.Open(this.VisitSynOpenDeclTarget target, range)
        | SynModuleDecl.Attributes(attrs, range) ->
            SynModuleDecl.Attributes(attrs |> List.map this.VisitSynAttributeList, range)
        | SynModuleDecl.HashDirective(hash, range) ->
            SynModuleDecl.HashDirective(this.VisitParsedHashDirective hash, range)
        | SynModuleDecl.NamespaceFragment(moduleOrNamespace) ->
            SynModuleDecl.NamespaceFragment(this.VisitSynModuleOrNamespace moduleOrNamespace)

    abstract VisitSynOpenDeclTarget: SynOpenDeclTarget -> SynOpenDeclTarget
    
    default this.VisitSynOpenDeclTarget(target: SynOpenDeclTarget): SynOpenDeclTarget =
        match target with
        | SynOpenDeclTarget.ModuleOrNamespace(longId, range) ->
            SynOpenDeclTarget.ModuleOrNamespace(this.VisitSynLongIdent(longId), range)
        | SynOpenDeclTarget.Type(typeName, range) ->
            SynOpenDeclTarget.Type(this.VisitSynType(typeName), range)    
    
    abstract VisitSynLongIdent: SynLongIdent -> SynLongIdent

    default this.VisitSynLongIdent(synLongIdent: SynLongIdent): SynLongIdent =
        match synLongIdent with
        | SynLongIdent(idents, dotRanges, identTriviaOptions) ->
            SynLongIdent(this.VisitLongIdent(idents), dotRanges, identTriviaOptions)

    abstract VisitSynExprAndBang: SynExprAndBang -> SynExprAndBang

    default this.VisitSynExprAndBang(synExpr: SynExprAndBang): SynExprAndBang =
        match synExpr with
        | SynExprAndBang(debugPointAtBinding, isUse, isFromSource, synPat, synExpr, range, synExprAndBangTrivia) ->
            SynExprAndBang(debugPointAtBinding, isUse, isFromSource, this.VisitSynPat synPat, this.VisitSynExpr synExpr, range, synExprAndBangTrivia)
        
    abstract VisitSynExpr: SynExpr -> SynExpr

    default this.VisitSynExpr(synExpr: SynExpr): SynExpr =
        match synExpr with
        | SynExpr.IndexRange(synExprOption, opm, exprOption, range1, range2, range3) ->
            SynExpr.IndexRange(synExprOption |> Option.map this.VisitSynExpr, opm, exprOption |> Option.map this.VisitSynExpr, range1, range2, range3)
        | SynExpr.IndexFromEnd(synExpr, range) -> SynExpr.IndexFromEnd(this.VisitSynExpr(synExpr), range)
        | SynExpr.Dynamic(funcExpr, qmark, argExpr, range) ->
            SynExpr.Dynamic(this.VisitSynExpr(funcExpr), qmark, this.VisitSynExpr(argExpr), range) 
        | SynExpr.DebugPoint(debugPointAtLeafExpr, isControlFlow, innerExpr) ->
            SynExpr.DebugPoint(debugPointAtLeafExpr, isControlFlow, this.VisitSynExpr(innerExpr))
        | SynExpr.Paren(expr, leftParenRange, rightParenRange, range) ->
            SynExpr.Paren(this.VisitSynExpr expr, leftParenRange, rightParenRange, range)
        | SynExpr.Quote(operator, isRaw, quotedSynExpr, isFromQueryExpression, range) ->
            SynExpr.Quote
                (this.VisitSynExpr operator, isRaw, this.VisitSynExpr quotedSynExpr, isFromQueryExpression, range)
        | SynExpr.Const(constant, range) -> SynExpr.Const(this.VisitSynConst constant, range)
        | SynExpr.Typed(expr, typeName, range) ->
            SynExpr.Typed(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Tuple(isStruct, exprs, commaRanges, range) ->
            SynExpr.Tuple(isStruct, exprs |> List.map this.VisitSynExpr, commaRanges, range)
        | SynExpr.ArrayOrList(isList, exprs, range) ->
            SynExpr.ArrayOrList(isList, exprs |> List.map this.VisitSynExpr, range)
        | SynExpr.Record(typInfo, copyInfo, recordFields, range) ->
            SynExpr.Record
                (typInfo
                 |> Option.map
                     (fun (typ, expr, leftParenRange, sep, rightParentRange) ->
                         (this.VisitSynType typ, this.VisitSynExpr expr, leftParenRange, sep, rightParentRange)),
                 copyInfo |> Option.map (fun (expr, opt) -> (this.VisitSynExpr expr, opt)),
                 recordFields |> List.map this.VisitRecordField, range)
        | SynExpr.AnonRecd(isStruct, copyInfo, recordFields, range) ->
            SynExpr.AnonRecd
                (isStruct, copyInfo |> Option.map (fun (expr, opt) -> (this.VisitSynExpr expr, opt)),
                 recordFields |> List.map this.VisitAnonRecordField, range)
        | SynExpr.New(isProtected, typeName, expr, range) ->
            SynExpr.New(isProtected, this.VisitSynType typeName, this.VisitSynExpr expr, range)
        | SynExpr.ObjExpr(objType, argOptions, withKeyword, bindings, members, extraImpls, newExprRange, range) ->
            SynExpr.ObjExpr
                (this.VisitSynType objType, Option.map this.VisitArgsOption argOptions, withKeyword,
                 bindings |> List.map this.VisitSynBinding, members |> List.map this.VisitSynMemberDefn,
                 extraImpls |> List.map this.VisitSynInterfaceImpl, newExprRange, range)
        | SynExpr.While(seqPoint, whileExpr, doExpr, range) ->
            SynExpr.While(seqPoint, this.VisitSynExpr whileExpr, this.VisitSynExpr doExpr, range)
        | SynExpr.For(forDebugPoint, toDebugPoint, ident, equalsRange, identBody, b, toBody, doBody, range) ->
            SynExpr.For
                (forDebugPoint, toDebugPoint, this.VisitIdent ident, equalsRange, this.VisitSynExpr identBody, b, this.VisitSynExpr toBody,
                 this.VisitSynExpr doBody, range)
        | SynExpr.ForEach(forDebugPoint, inDebugPoint, seqExprOnly, isFromSource, pat, enumExpr, bodyExpr, range) ->
            SynExpr.ForEach
                (forDebugPoint, inDebugPoint, seqExprOnly, isFromSource, this.VisitSynPat pat, this.VisitSynExpr enumExpr,
                 this.VisitSynExpr bodyExpr, range)
        | SynExpr.ArrayOrListComputed(isArray, expr, range) ->
            SynExpr.ArrayOrListComputed(isArray, this.VisitSynExpr expr, range)
        | SynExpr.ComputationExpr(hasSeqBuilder, expr, range) ->
            SynExpr.ComputationExpr(hasSeqBuilder, this.VisitSynExpr expr, range)
        | SynExpr.Lambda(fromMethod, inLambdaSeq, args, body, parsedData, range, trivia) ->
            SynExpr.Lambda(fromMethod, inLambdaSeq, this.VisitSynSimplePats args, this.VisitSynExpr body,
                           parsedData |> Option.map (fun (pats, expr) -> List.map this.VisitSynPat pats, this.VisitSynExpr expr), range, trivia)
        | SynExpr.MatchLambda(isExnMatch, r, matchClaseus, seqPoint, range) ->
            SynExpr.MatchLambda(isExnMatch, r, matchClaseus |> List.map this.VisitSynMatchClause, seqPoint, range)
        | SynExpr.Match(seqPoint, expr, clauses, range, trivia) ->
            SynExpr.Match(seqPoint, this.VisitSynExpr expr, clauses |> List.map this.VisitSynMatchClause, range, trivia)
        | SynExpr.Do(expr, range) -> SynExpr.Do(this.VisitSynExpr expr, range)
        | SynExpr.Assert(expr, range) -> SynExpr.Assert(this.VisitSynExpr expr, range)
        | SynExpr.App(atomicFlag, isInfix, funcExpr, argExpr, range) ->
            SynExpr.App(atomicFlag, isInfix, this.VisitSynExpr funcExpr, this.VisitSynExpr argExpr, range)
        | SynExpr.TypeApp(expr, lESSrange, typeNames, commaRanges, gREATERrange, typeArgsRange, range) ->
            SynExpr.TypeApp
                (this.VisitSynExpr expr, lESSrange, typeNames |> List.map this.VisitSynType, commaRanges, gREATERrange,
                 typeArgsRange, range)
        | SynExpr.LetOrUse(isRecursive, isUse, bindings, body, range, trivia) ->
            SynExpr.LetOrUse
                (isRecursive, isUse, bindings |> List.map this.VisitSynBinding, this.VisitSynExpr body, range, trivia)
        | SynExpr.TryWith(tryExpr, withCases, withRange, range, trySeqPoint, withSeqPoint) ->
            SynExpr.TryWith
                (this.VisitSynExpr tryExpr, withCases |> List.map this.VisitSynMatchClause, withRange, range,
                 trySeqPoint, withSeqPoint)
        | SynExpr.TryFinally(tryExpr, finallyExpr, range, trySeqPoint, withSeqPoint, trivia) ->
            SynExpr.TryFinally
                (this.VisitSynExpr tryExpr, this.VisitSynExpr finallyExpr, range, trySeqPoint, withSeqPoint, trivia)
        | SynExpr.Lazy(ex, range) -> SynExpr.Lazy(this.VisitSynExpr ex, range)
        | SynExpr.Sequential(seqPoint, isTrueSeq, expr1, expr2, range) ->
            SynExpr.Sequential(seqPoint, isTrueSeq, this.VisitSynExpr expr1, this.VisitSynExpr expr2, range)
        | SynExpr.SequentialOrImplicitYield(seqPoint, expr1, expr2, ifNotStmt, range) ->
            SynExpr.SequentialOrImplicitYield
                (seqPoint, this.VisitSynExpr expr1, this.VisitSynExpr expr2, this.VisitSynExpr ifNotStmt, range)
        | SynExpr.IfThenElse(ifExpr, thenExpr, elseExpr, seqPoint, isFromErrorRecovery, ifToThenRange, range) ->
            SynExpr.IfThenElse
                (this.VisitSynExpr ifExpr, this.VisitSynExpr thenExpr, Option.map this.VisitSynExpr elseExpr, seqPoint,
                 isFromErrorRecovery, ifToThenRange, range)
        | SynExpr.Ident(id) -> SynExpr.Ident(this.VisitIdent id)
        | SynExpr.LongIdent(isOptional, longDotId, seqPoint, range) ->
            SynExpr.LongIdent(isOptional, this.VisitSynLongIdent longDotId, seqPoint, range)
        | SynExpr.LongIdentSet(longDotId, expr, range) ->
            SynExpr.LongIdentSet(this.VisitSynLongIdent longDotId, this.VisitSynExpr expr, range)
        | SynExpr.DotGet(expr, rangeOfDot, longDotId, range) ->
            SynExpr.DotGet(this.VisitSynExpr expr, rangeOfDot, this.VisitSynLongIdent longDotId, range)
        | SynExpr.DotSet(expr, longDotId, e2, range) ->
            SynExpr.DotSet
                (this.VisitSynExpr expr, this.VisitSynLongIdent longDotId, this.VisitSynExpr e2, range)
        | SynExpr.Set(e1, e2, range) -> SynExpr.Set(this.VisitSynExpr e1, this.VisitSynExpr e2, range)
        | SynExpr.DotIndexedGet(objectExpr, indexExprs, dotRange, range) ->
            SynExpr.DotIndexedGet
                (this.VisitSynExpr objectExpr, this.VisitSynExpr indexExprs, dotRange, range)
        | SynExpr.DotIndexedSet(objectExpr, indexExprs, valueExpr, leftOfSetRange, dotRange, range) ->
            SynExpr.DotIndexedSet
                (this.VisitSynExpr objectExpr, this.VisitSynExpr indexExprs,
                 this.VisitSynExpr valueExpr, leftOfSetRange, dotRange, range)
        | SynExpr.NamedIndexedPropertySet(longDotId, e1, e2, range) ->
            SynExpr.NamedIndexedPropertySet
                (this.VisitSynLongIdent longDotId, this.VisitSynExpr e1, this.VisitSynExpr e2, range)
        | SynExpr.DotNamedIndexedPropertySet(expr, longDotId, e1, e2, range) ->
            SynExpr.DotNamedIndexedPropertySet
                (this.VisitSynExpr expr, this.VisitSynLongIdent longDotId, this.VisitSynExpr e1,
                 this.VisitSynExpr e2, range)
        | SynExpr.TypeTest(expr, typeName, range) ->
            SynExpr.TypeTest(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Upcast(expr, typeName, range) ->
            SynExpr.Upcast(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Downcast(expr, typeName, range) ->
            SynExpr.Downcast(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.InferredUpcast(expr, range) -> SynExpr.InferredUpcast(this.VisitSynExpr expr, range)
        | SynExpr.InferredDowncast(expr, range) -> SynExpr.InferredDowncast(this.VisitSynExpr expr, range)
        | SynExpr.Null(range) -> SynExpr.Null(range)
        | SynExpr.AddressOf(isByref, expr, refRange, range) ->
            SynExpr.AddressOf(isByref, this.VisitSynExpr expr, refRange, range)
        | SynExpr.TraitCall(supportTys, sign, expr, range) ->
            SynExpr.TraitCall(this.VisitSynType supportTys, this.VisitSynMemberSig sign, this.VisitSynExpr expr, range)
        | SynExpr.JoinIn(expr, inrange, expr2, range) ->
            SynExpr.JoinIn(this.VisitSynExpr expr, inrange, this.VisitSynExpr expr2, range)
        | SynExpr.ImplicitZero(range) -> SynExpr.ImplicitZero(range)
        | SynExpr.YieldOrReturn(info, expr, range) -> SynExpr.YieldOrReturn(info, this.VisitSynExpr expr, range)
        | SynExpr.YieldOrReturnFrom(info, expr, range) ->
            SynExpr.YieldOrReturnFrom(info, this.VisitSynExpr expr, range)
        | SynExpr.LetOrUseBang(seqPoint, isUse, isFromSource, pat, rhsExpr, andBangs, bodyExpr, range, trivia) ->
            SynExpr.LetOrUseBang
                (seqPoint, isUse, isFromSource, this.VisitSynPat pat, this.VisitSynExpr rhsExpr,
                 andBangs |> List.map this.VisitSynExprAndBang, this.VisitSynExpr bodyExpr, range, trivia)
        | SynExpr.MatchBang(seqPoint, expr, clauses, range, trivia) ->
            SynExpr.MatchBang(seqPoint, this.VisitSynExpr expr, clauses |> List.map this.VisitSynMatchClause, range, trivia)
        | SynExpr.DoBang(expr, range) -> SynExpr.DoBang(this.VisitSynExpr expr, range)
        | SynExpr.LibraryOnlyILAssembly(a, typs, exprs, typs2, range) ->
            SynExpr.LibraryOnlyILAssembly
                (a, List.map this.VisitSynType typs, List.map this.VisitSynExpr exprs,
                 List.map this.VisitSynType typs2, range)
        | SynExpr.LibraryOnlyStaticOptimization(constraints, expr1, expr2, range) ->
            SynExpr.LibraryOnlyStaticOptimization
                (constraints, this.VisitSynExpr expr1, this.VisitSynExpr expr2, range)
        | SynExpr.LibraryOnlyUnionCaseFieldGet(expr, longId, i, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldGet(this.VisitSynExpr expr, this.VisitLongIdent longId, i, range)
        | SynExpr.LibraryOnlyUnionCaseFieldSet(e1, longId, i, e2, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldSet
                (this.VisitSynExpr e1, this.VisitLongIdent longId, i, this.VisitSynExpr e2, range)
        | SynExpr.ArbitraryAfterError(debugStr, range) -> SynExpr.ArbitraryAfterError(debugStr, range)
        | SynExpr.FromParseError(expr, range) -> SynExpr.FromParseError(this.VisitSynExpr expr, range)
        | SynExpr.DiscardAfterMissingQualificationAfterDot(expr, range) ->
            SynExpr.DiscardAfterMissingQualificationAfterDot(this.VisitSynExpr expr, range)
        | SynExpr.Fixed(expr, range) -> SynExpr.Fixed(this.VisitSynExpr expr, range)
        | SynExpr.InterpolatedString(contents, kind, range) -> SynExpr.InterpolatedString(List.map this.VisitSynInterpolatedStringPart contents, kind, range)
        | SynExpr.Typar(synTypar, range) -> SynExpr.Typar(this.VisitSynTypar synTypar, range)

    abstract VisitSynInterpolatedStringPart: SynInterpolatedStringPart -> SynInterpolatedStringPart
    
    default this.VisitSynInterpolatedStringPart(part: SynInterpolatedStringPart): SynInterpolatedStringPart =
        match part with
        | SynInterpolatedStringPart.String(value, range) -> SynInterpolatedStringPart.String(value, range) 
        | SynInterpolatedStringPart.FillExpr(fillExpr, qualifiers) -> SynInterpolatedStringPart.FillExpr(this.VisitSynExpr fillExpr, Option.map this.VisitIdent qualifiers)

    abstract VisitRecordField: SynExprRecordField -> SynExprRecordField
    default this.VisitRecordField(recordField: SynExprRecordField) =
        match recordField with
        | SynExprRecordField(fieldName, equalsRange, synExprOption, blockSeparator) ->
            SynExprRecordField(this.VisitRecordFieldName fieldName, equalsRange, Option.map this.VisitSynExpr synExprOption, blockSeparator)

    abstract VisitRecordFieldName: RecordFieldName -> RecordFieldName
    default this.VisitRecordFieldName((ident: SynLongIdent, correct: bool)) = (this.VisitSynLongIdent ident, correct)

    abstract VisitAnonRecordField: (Ident * range option * SynExpr) -> Ident * range option * SynExpr
    default this.VisitAnonRecordField((ident: Ident, r: range option, expr: SynExpr)) = (this.VisitIdent ident, r, this.VisitSynExpr expr)

    abstract VisitAnonRecordTypeField: (Ident * SynType) -> Ident * SynType
    default this.VisitAnonRecordTypeField((ident: Ident, t: SynType)) = (this.VisitIdent ident, this.VisitSynType t)

    abstract VisitSynMemberSig: SynMemberSig -> SynMemberSig

    default this.VisitSynMemberSig(ms: SynMemberSig): SynMemberSig =
        match ms with
        | SynMemberSig.Member(valSig, flags, range) -> SynMemberSig.Member(this.VisitSynValSig valSig, flags, range)
        | SynMemberSig.Interface(typeName, range) -> SynMemberSig.Interface(this.VisitSynType typeName, range)
        | SynMemberSig.Inherit(typeName, range) -> SynMemberSig.Inherit(this.VisitSynType typeName, range)
        | SynMemberSig.ValField(f, range) -> SynMemberSig.ValField(this.VisitSynField f, range)
        | SynMemberSig.NestedType(typedef, range) -> SynMemberSig.NestedType(this.VisitSynTypeDefnSig typedef, range)

    abstract VisitSynMatchClause: SynMatchClause -> SynMatchClause

    default this.VisitSynMatchClause(mc: SynMatchClause): SynMatchClause =
        match mc with
        | SynMatchClause(synPat, synExprOption, resultExpr, range, debugPointAtTarget, synMatchClauseTrivia) ->
            SynMatchClause(this.VisitSynPat synPat, Option.map this.VisitSynExpr synExprOption, this.VisitSynExpr resultExpr, range, debugPointAtTarget, synMatchClauseTrivia)

    abstract VisitArgsOption: (SynExpr * Ident option) -> SynExpr * Ident option
    default this.VisitArgsOption((expr: SynExpr, ident: Ident option)) =
        (this.VisitSynExpr expr, Option.map this.VisitIdent ident)
    
    abstract VisitSynInterfaceImpl: SynInterfaceImpl -> SynInterfaceImpl

    default this.VisitSynInterfaceImpl(ii: SynInterfaceImpl): SynInterfaceImpl =
        match ii with
        | SynInterfaceImpl(interfaceTy, withKeyword, synBindings, synMemberDefns, range) ->
            SynInterfaceImpl(this.VisitSynType interfaceTy, withKeyword, synBindings |> List.map this.VisitSynBinding, synMemberDefns |> List.map this.VisitSynMemberDefn, range)

    abstract VisitSynTypeDefn: SynTypeDefn -> SynTypeDefn

    default this.VisitSynTypeDefn(td: SynTypeDefn): SynTypeDefn =
        match td with
        | SynTypeDefn(synComponentInfo, synTypeDefnRepr, synMemberDefns, synMemberDefnOption, range, synTypeDefnTrivia) ->
            SynTypeDefn(this.VisitSynComponentInfo synComponentInfo, this.VisitSynTypeDefnRepr synTypeDefnRepr, synMemberDefns |> List.map this.VisitSynMemberDefn, synMemberDefnOption |> Option.map this.VisitSynMemberDefn, range, synTypeDefnTrivia)

    abstract VisitSynTypeDefnSig: SynTypeDefnSig -> SynTypeDefnSig

    default this.VisitSynTypeDefnSig(typeDefSig: SynTypeDefnSig): SynTypeDefnSig =
        match typeDefSig with
        | SynTypeDefnSig(synComponentInfo, synTypeDefnSigRepr, synMemberSigs, range, synTypeDefnSigTrivia) ->
            SynTypeDefnSig(this.VisitSynComponentInfo synComponentInfo, this.VisitSynTypeDefnSigRepr synTypeDefnSigRepr, synMemberSigs |> List.map this.VisitSynMemberSig, range, synTypeDefnSigTrivia)

    abstract VisitSynTypeDefnSigRepr: SynTypeDefnSigRepr -> SynTypeDefnSigRepr

    default this.VisitSynTypeDefnSigRepr(stdr: SynTypeDefnSigRepr): SynTypeDefnSigRepr =
        match stdr with
        | SynTypeDefnSigRepr.ObjectModel(kind, members, range) ->
            SynTypeDefnSigRepr.ObjectModel
                (this.VisitSynTypeDefnKind kind, members |> List.map this.VisitSynMemberSig, range)
        | SynTypeDefnSigRepr.Simple(simpleRepr, range) ->
            SynTypeDefnSigRepr.Simple(this.VisitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnSigRepr.Exception(exceptionRepr) ->
            SynTypeDefnSigRepr.Exception(this.VisitSynExceptionDefnRepr exceptionRepr)

    abstract VisitSynMemberDefn: SynMemberDefn -> SynMemberDefn

    default this.VisitSynMemberDefn(mbrDef: SynMemberDefn): SynMemberDefn =
        match mbrDef with
        | SynMemberDefn.GetSetMember(memberDefnForGet, memberDefnForSet, range1, synMemberGetSetTrivia) ->
            SynMemberDefn.GetSetMember(memberDefnForGet |> Option.map this.VisitSynBinding, memberDefnForSet |> Option.map this.VisitSynBinding, range1, synMemberGetSetTrivia)
        | SynMemberDefn.Open(target, range) -> SynMemberDefn.Open(this.VisitSynOpenDeclTarget target, range)
        | SynMemberDefn.Member(memberDefn, range) -> SynMemberDefn.Member(this.VisitSynBinding memberDefn, range)
        | SynMemberDefn.ImplicitCtor(access, attrs, ctorArgs, selfIdentifier, doc, range) ->
            SynMemberDefn.ImplicitCtor
                (Option.map this.VisitSynAccess access, attrs |> List.map this.VisitSynAttributeList,
                 this.VisitSynSimplePats ctorArgs, Option.map this.VisitIdent selfIdentifier, this.VisitPreXmlDoc(doc), range)
        | SynMemberDefn.ImplicitInherit(inheritType, inheritArgs, inheritAlias, range) ->
            SynMemberDefn.ImplicitInherit
                (this.VisitSynType inheritType, this.VisitSynExpr inheritArgs, Option.map this.VisitIdent inheritAlias,
                 range)
        | SynMemberDefn.LetBindings(bindings, isStatic, isRecursive, range) ->
            SynMemberDefn.LetBindings(bindings |> List.map this.VisitSynBinding, isStatic, isRecursive, range)
        | SynMemberDefn.AbstractSlot(valSig, flags, range) ->
            SynMemberDefn.AbstractSlot(this.VisitSynValSig valSig, flags, range)
        | SynMemberDefn.Interface(typ, withKeyword, members, range) ->
            SynMemberDefn.Interface
                (this.VisitSynType typ, withKeyword, Option.map (List.map this.VisitSynMemberDefn) members, range)
        | SynMemberDefn.Inherit(typ, ident, range) ->
            SynMemberDefn.Inherit(this.VisitSynType typ, Option.map this.VisitIdent ident, range)
        | SynMemberDefn.ValField(fld, range) -> SynMemberDefn.ValField(this.VisitSynField fld, range)
        | SynMemberDefn.NestedType(typeDefn, access, range) ->
            SynMemberDefn.NestedType(this.VisitSynTypeDefn typeDefn, Option.map this.VisitSynAccess access, range)
        | SynMemberDefn.AutoProperty(attrs, isStatic, ident, typeOpt, propKind, flags, flagsForSet, doc, access, synExpr, range, trivia) ->
            SynMemberDefn.AutoProperty
                (attrs |> List.map this.VisitSynAttributeList, isStatic, this.VisitIdent ident,
                 Option.map this.VisitSynType typeOpt, propKind, flags, flagsForSet, this.VisitPreXmlDoc(doc), Option.map this.VisitSynAccess access,
                 this.VisitSynExpr synExpr, range, trivia)

    abstract VisitSynSimplePat: SynSimplePat -> SynSimplePat

    default this.VisitSynSimplePat(sp: SynSimplePat): SynSimplePat =
        match sp with
        | SynSimplePat.Id(ident, altName, isCompilerGenerated, isThisVar, isOptArg, range) ->
            SynSimplePat.Id(this.VisitIdent ident, altName, isCompilerGenerated, isThisVar, isOptArg, range)
        | SynSimplePat.Typed(simplePat, typ, range) ->
            SynSimplePat.Typed(this.VisitSynSimplePat simplePat, this.VisitSynType typ, range)
        | SynSimplePat.Attrib(simplePat, attrs, range) ->
            SynSimplePat.Attrib(this.VisitSynSimplePat simplePat, attrs |> List.map this.VisitSynAttributeList, range)

    abstract VisitSynSimplePats: SynSimplePats -> SynSimplePats

    default this.VisitSynSimplePats(sp: SynSimplePats): SynSimplePats =
        match sp with
        | SynSimplePats.SimplePats(pats, range) ->
            SynSimplePats.SimplePats(pats |> List.map this.VisitSynSimplePat, range)
        | SynSimplePats.Typed(pats, typ, range) ->
            SynSimplePats.Typed(this.VisitSynSimplePats pats, this.VisitSynType typ, range)

    abstract VisitSynBinding: SynBinding -> SynBinding

    default this.VisitSynBinding(binding: SynBinding): SynBinding =
        match binding with
        | SynBinding(access, kind, mustInline, isMutable, attrs, doc, valData, headPat, returnInfo, expr, range, seqPoint, trivia) ->
            SynBinding
                (Option.map this.VisitSynAccess access, kind, mustInline, isMutable,
                 attrs |> List.map this.VisitSynAttributeList, this.VisitPreXmlDoc(doc), this.VisitSynValData valData,
                 this.VisitSynPat headPat, Option.map this.VisitSynBindingReturnInfo returnInfo, this.VisitSynExpr expr,
                 range, seqPoint, trivia)

    abstract VisitSynValData: SynValData -> SynValData

    default this.VisitSynValData(svd: SynValData): SynValData =
        match svd with
        | SynValData(flags, svi, ident) ->
            SynValData(flags, this.VisitSynValInfo svi, Option.map this.VisitIdent ident)

    abstract VisitSynValSig: SynValSig -> SynValSig

    default this.VisitSynValSig(svs: SynValSig): SynValSig =
        match svs with
        | SynValSig(attrs, ident, explicitValDecls, synType, arity, isInline, isMutable, doc, access, expr, range, trivia) ->
            SynValSig
                (attrs |> List.map this.VisitSynAttributeList, this.VisitSynIdent ident,
                 this.VisitSynValTyparDecls explicitValDecls, this.VisitSynType synType, this.VisitSynValInfo arity,
                 isInline, isMutable, this.VisitPreXmlDoc(doc), Option.map this.VisitSynAccess access, Option.map this.VisitSynExpr expr,
                 range, trivia)

    abstract VisitSynValTyparDecls: SynValTyparDecls -> SynValTyparDecls

    default this.VisitSynValTyparDecls(valTypeDecl: SynValTyparDecls): SynValTyparDecls =
        match valTypeDecl with
        | SynValTyparDecls(typardecls, b) ->
            SynValTyparDecls(typardecls |> Option.map this.VisitSynTyparDecls, b)
            
    abstract VisitSynTyparDecls: SynTyparDecls -> SynTyparDecls

    default this.VisitSynTyparDecls(typarDecls: SynTyparDecls): SynTyparDecls =
        match typarDecls with
        | SynTyparDecls.PostfixList(synTyparDecls, synTypeConstraints, range) ->
            SynTyparDecls.PostfixList(List.map this.VisitSynTyparDecl synTyparDecls, synTypeConstraints, range)
        | SynTyparDecls.PrefixList(synTyparDecls, range) ->
            SynTyparDecls.PrefixList(List.map this.VisitSynTyparDecl synTyparDecls, range)
        | SynTyparDecls.SinglePrefix(synTyparDecl, range) ->
            SynTyparDecls.SinglePrefix(this.VisitSynTyparDecl synTyparDecl, range)

    abstract VisitSynTyparDecl: SynTyparDecl -> SynTyparDecl

    default this.VisitSynTyparDecl(std: SynTyparDecl): SynTyparDecl =
        match std with
        | SynTyparDecl(attrs, typar) -> SynTyparDecl(attrs |> List.map this.VisitSynAttributeList, this.VisitSynTypar typar)

    abstract VisitSynTypar: SynTypar -> SynTypar

    default this.VisitSynTypar(typar: SynTypar): SynTypar =
        match typar with
        | SynTypar(ident, staticReq, isComGen) -> SynTypar(this.VisitIdent ident, staticReq, isComGen)

    abstract VisitTyparStaticReq: TyparStaticReq -> TyparStaticReq

    default this.VisitTyparStaticReq(tsr: TyparStaticReq): TyparStaticReq = tsr

    abstract VisitSynBindingReturnInfo: SynBindingReturnInfo -> SynBindingReturnInfo

    default this.VisitSynBindingReturnInfo(returnInfo: SynBindingReturnInfo): SynBindingReturnInfo =
        match returnInfo with
        | SynBindingReturnInfo(typeName, range, attrs, trivia) ->
            SynBindingReturnInfo(this.VisitSynType typeName, range, attrs |> List.map this.VisitSynAttributeList, trivia)

    abstract VisitSynPat: SynPat -> SynPat

    default this.VisitSynPat(sp: SynPat): SynPat =
        match sp with
        | SynPat.As(lhsPat, rhsPat, range1) ->SynPat.As(this.VisitSynPat(lhsPat), this.VisitSynPat(rhsPat), range1) 
        | SynPat.Const(sc, range) -> SynPat.Const(this.VisitSynConst sc, range)
        | SynPat.Wild(range) -> SynPat.Wild(range)
        | SynPat.Named(ident, isSelfIdentifier, access, range) ->
            SynPat.Named (this.VisitSynIdent ident, isSelfIdentifier, Option.map this.VisitSynAccess access, range)
        | SynPat.Typed(synPat, synType, range) ->
            SynPat.Typed(this.VisitSynPat synPat, this.VisitSynType synType, range)
        | SynPat.Attrib(synPat, attrs, range) ->
            SynPat.Attrib(this.VisitSynPat synPat, attrs |> List.map this.VisitSynAttributeList, range)
        | SynPat.Or(synPat, synPat2, range, trivia) -> SynPat.Or(this.VisitSynPat synPat, this.VisitSynPat synPat2, range, trivia)
        | SynPat.Ands(pats, range) -> SynPat.Ands(pats |> List.map this.VisitSynPat, range)
        | SynPat.LongIdent(longDotId, ident, svtd, ctorArgs, access, range) ->
            SynPat.LongIdent
                (this.VisitSynLongIdent longDotId, Option.map this.VisitIdent ident,
                 Option.map this.VisitSynValTyparDecls svtd, this.VisitSynArgPats ctorArgs,
                 Option.map this.VisitSynAccess access, range)
        | SynPat.Tuple(isStruct, pats, range) -> SynPat.Tuple(isStruct, pats |> List.map this.VisitSynPat, range)
        | SynPat.Paren(pat, range) -> SynPat.Paren(this.VisitSynPat pat, range)
        | SynPat.ArrayOrList(isList, pats, range) ->
            SynPat.ArrayOrList(isList, pats |> List.map this.VisitSynPat, range)
        | SynPat.Record(pats, range) ->
            SynPat.Record
                (pats
                 |> List.map
                     (fun ((longIdent, ident), r, pat) ->
                         ((this.VisitLongIdent longIdent, this.VisitIdent ident), r, this.VisitSynPat pat)), range)
        | SynPat.Null(range) -> SynPat.Null(range)
        | SynPat.OptionalVal(ident, range) -> SynPat.OptionalVal(this.VisitIdent ident, range)
        | SynPat.IsInst(typ, range) -> SynPat.IsInst(this.VisitSynType typ, range)
        | SynPat.QuoteExpr(expr, range) -> SynPat.QuoteExpr(this.VisitSynExpr expr, range)
        | SynPat.DeprecatedCharRange(c, c2, range) -> SynPat.DeprecatedCharRange(c, c2, range)
        | SynPat.InstanceMember(ident, ident2, ident3, access, range) ->
            SynPat.InstanceMember
                (this.VisitIdent ident, this.VisitIdent ident2, Option.map this.VisitIdent ident3,
                 Option.map this.VisitSynAccess access, range)
        | SynPat.FromParseError(pat, range) -> SynPat.FromParseError(this.VisitSynPat pat, range)
        | SynPat.ListCons(lhsPat, rhsPat, range, synPatListConsTrivia) ->
            SynPat.ListCons(this.VisitSynPat lhsPat, this.VisitSynPat rhsPat, range, synPatListConsTrivia)

    abstract VisitSynArgPats: SynArgPats -> SynArgPats

    default this.VisitSynArgPats(ctorArgs: SynArgPats): SynArgPats =
        match ctorArgs with
        | SynArgPats.Pats(pats) -> SynArgPats.Pats(pats |> List.map this.VisitSynPat)
        | SynArgPats.NamePatPairs(pats, range, trivia) ->
            SynArgPats.NamePatPairs(pats |> List.map (fun (ident, r, pat) -> (this.VisitIdent ident, r, this.VisitSynPat pat)), range, trivia)

    abstract VisitSynComponentInfo: SynComponentInfo -> SynComponentInfo

    default this.VisitSynComponentInfo(sci: SynComponentInfo): SynComponentInfo =
        match sci with
        | SynComponentInfo(attribs, typeParams, constraints, longId, doc, preferPostfix, access, range) ->
            SynComponentInfo
                (attribs |> List.map this.VisitSynAttributeList, Option.map this.VisitSynTyparDecls typeParams,
                 constraints, longId, this.VisitPreXmlDoc(doc), preferPostfix, Option.map this.VisitSynAccess access, range)

    abstract VisitSynTypeDefnRepr: SynTypeDefnRepr -> SynTypeDefnRepr

    default this.VisitSynTypeDefnRepr(stdr: SynTypeDefnRepr): SynTypeDefnRepr =
        match stdr with
        | SynTypeDefnRepr.ObjectModel(kind, members, range) ->
            SynTypeDefnRepr.ObjectModel
                (this.VisitSynTypeDefnKind kind, members |> List.map this.VisitSynMemberDefn, range)
        | SynTypeDefnRepr.Simple(simpleRepr, range) ->
            SynTypeDefnRepr.Simple(this.VisitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnRepr.Exception(exceptionRepr) ->
            SynTypeDefnRepr.Exception(this.VisitSynExceptionDefnRepr exceptionRepr)

    abstract VisitSynTypeDefnKind: SynTypeDefnKind -> SynTypeDefnKind

    default this.VisitSynTypeDefnKind(kind: SynTypeDefnKind): SynTypeDefnKind =
        match kind with
        | SynTypeDefnKind.Augmentation(withKeyword) -> SynTypeDefnKind.Augmentation(withKeyword)
        | SynTypeDefnKind.Delegate(signature, signatureInfo) -> SynTypeDefnKind.Delegate(this.VisitSynType signature, this.VisitSynValInfo signatureInfo)
        | _ -> kind

    abstract VisitSynTypeDefnSimpleRepr: SynTypeDefnSimpleRepr -> SynTypeDefnSimpleRepr

    default this.VisitSynTypeDefnSimpleRepr(arg: SynTypeDefnSimpleRepr): SynTypeDefnSimpleRepr =
        match arg with
        | SynTypeDefnSimpleRepr.None(range) -> SynTypeDefnSimpleRepr.None(range)
        | SynTypeDefnSimpleRepr.Union(access, unionCases, range) ->
            SynTypeDefnSimpleRepr.Union
                (Option.map this.VisitSynAccess access, unionCases |> List.map this.VisitSynUnionCase, range)
        | SynTypeDefnSimpleRepr.Enum(enumCases, range) ->
            SynTypeDefnSimpleRepr.Enum(enumCases |> List.map this.VisitSynEnumCase, range)
        | SynTypeDefnSimpleRepr.Record(access, recordFields, range) ->
            SynTypeDefnSimpleRepr.Record
                (Option.map this.VisitSynAccess access, recordFields |> List.map this.VisitSynField, range)
        | SynTypeDefnSimpleRepr.General(typeDefKind, a, b, c, d, e, pats, range) ->
            SynTypeDefnSimpleRepr.General(this.VisitSynTypeDefnKind typeDefKind, a, b, c, d, e, pats, range) // TODO
        | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(ilType, range) ->
            SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(ilType, range)
        | SynTypeDefnSimpleRepr.TypeAbbrev(parserDetail, typ, range) ->
            SynTypeDefnSimpleRepr.TypeAbbrev(parserDetail, this.VisitSynType typ, range)
        | SynTypeDefnSimpleRepr.Exception(edr) -> SynTypeDefnSimpleRepr.Exception(this.VisitSynExceptionDefnRepr edr)

    abstract VisitSynExceptionDefn: SynExceptionDefn -> SynExceptionDefn

    default this.VisitSynExceptionDefn(exceptionDef: SynExceptionDefn): SynExceptionDefn =
        match exceptionDef with
        | SynExceptionDefn(sedr, withKeyword, members, range) ->
            SynExceptionDefn(this.VisitSynExceptionDefnRepr sedr, withKeyword, members |> List.map this.VisitSynMemberDefn, range)

    abstract VisitSynExceptionDefnRepr: SynExceptionDefnRepr -> SynExceptionDefnRepr

    default this.VisitSynExceptionDefnRepr(sedr: SynExceptionDefnRepr): SynExceptionDefnRepr =
        match sedr with
        | SynExceptionDefnRepr(attrs, unionCase, longId, doc, access, range) ->
            SynExceptionDefnRepr
                (attrs |> List.map this.VisitSynAttributeList, this.VisitSynUnionCase unionCase, longId, this.VisitPreXmlDoc(doc),
                 Option.map this.VisitSynAccess access, range)

    abstract VisitSynAttribute: SynAttribute -> SynAttribute

    default this.VisitSynAttribute(attr: SynAttribute): SynAttribute =
        { attr with
              ArgExpr = this.VisitSynExpr attr.ArgExpr
              Target = Option.map this.VisitIdent attr.Target }

    abstract VisitSynAttributeList: SynAttributeList -> SynAttributeList
    default this.VisitSynAttributeList(attrs: SynAttributeList): SynAttributeList =
        { attrs with Attributes = attrs.Attributes |> List.map this.VisitSynAttribute }

    abstract VisitSynUnionCase: SynUnionCase -> SynUnionCase

    default this.VisitSynUnionCase(uc: SynUnionCase): SynUnionCase =
        match uc with
        | SynUnionCase(attrs, ident, uct, doc, access, range, trivia) ->
            SynUnionCase
                (attrs |> List.map this.VisitSynAttributeList, this.VisitSynIdent ident, uct,
                 this.VisitPreXmlDoc(doc), Option.map this.VisitSynAccess access, range, trivia)

    abstract VisitSynEnumCase: SynEnumCase -> SynEnumCase

    default this.VisitSynEnumCase(sec: SynEnumCase): SynEnumCase =
        match sec with
        | SynEnumCase(attrs, ident, cnst, cnstRange, doc, trivia, range) ->
            SynEnumCase
                (attrs |> List.map this.VisitSynAttributeList, this.VisitSynIdent ident, this.VisitSynConst cnst,
                 cnstRange, this.VisitPreXmlDoc(doc), trivia, range)

    abstract VisitSynField: SynField -> SynField

    default this.VisitSynField(sfield: SynField): SynField =
        match sfield with
        | SynField(attrs, isStatic, ident, typ, isMutable, doc, access, range, trivia) ->
            SynField
                (attrs |> List.map this.VisitSynAttributeList, isStatic, Option.map this.VisitIdent ident,
                 this.VisitSynType typ, isMutable, this.VisitPreXmlDoc(doc), Option.map this.VisitSynAccess access, range, trivia)

    abstract VisitSynType: SynType -> SynType

    default this.VisitSynType(st: SynType): SynType =
        match st with
        | SynType.Paren(innerType, range) -> SynType.Paren(this.VisitSynType innerType, range)
        | SynType.LongIdent(li) -> SynType.LongIdent(li)
        | SynType.App(typeName, lessRange, typeArgs, commaRanges, greaterRange, isPostfix, range) ->
            SynType.App
                (this.VisitSynType typeName, lessRange, typeArgs |> List.map this.VisitSynType, commaRanges,
                 greaterRange, isPostfix, range)
        | SynType.LongIdentApp(typeName, longDotId, lessRange, typeArgs, commaRanges, greaterRange, range) ->
            SynType.LongIdentApp
                (this.VisitSynType typeName, longDotId, lessRange, typeArgs |> List.map this.VisitSynType, commaRanges,
                 greaterRange, range)
        | SynType.Tuple(isStruct, typeNames, range) ->
            SynType.Tuple(isStruct, typeNames |> List.map this.VisitSynTupleTypeSegment, range)
        | SynType.Array(i, elementType, range) -> SynType.Array(i, this.VisitSynType elementType, range)
        | SynType.Fun(argType, returnType, range, trivia) ->
            SynType.Fun(this.VisitSynType argType, this.VisitSynType returnType, range, trivia)
        | SynType.Var(genericName, range) -> SynType.Var(this.VisitSynTypar genericName, range)
        | SynType.Anon(range) -> SynType.Anon(range)
        | SynType.WithGlobalConstraints(typeName, constraints, range) ->
            SynType.WithGlobalConstraints(this.VisitSynType typeName, constraints, range)
        | SynType.HashConstraint(synType, range) -> SynType.HashConstraint(this.VisitSynType synType, range)
        | SynType.MeasureDivide(dividendType, divisorType, range) ->
            SynType.MeasureDivide(this.VisitSynType dividendType, this.VisitSynType divisorType, range)
        | SynType.MeasurePower(measureType, cnst, range) ->
            SynType.MeasurePower(this.VisitSynType measureType, cnst, range)
        | SynType.StaticConstant(constant, range) -> SynType.StaticConstant(this.VisitSynConst constant, range)
        | SynType.StaticConstantExpr(expr, range) -> SynType.StaticConstantExpr(this.VisitSynExpr expr, range)
        | SynType.StaticConstantNamed(expr, typ, range) ->
            SynType.StaticConstantNamed(this.VisitSynType expr, this.VisitSynType typ, range)
        | SynType.AnonRecd(isStruct, typeNames, range) ->
            SynType.AnonRecd(isStruct, List.map this.VisitAnonRecordTypeField typeNames, range)
        | SynType.Or(lhsType, rhsType, range, synTypeOrTrivia) ->
            SynType.Or(this.VisitSynType lhsType, this.VisitSynType rhsType, range, this.VisitSynTypeOrTrivia synTypeOrTrivia)
        | SynType.SignatureParameter(synAttributeLists, optional, identOption, usedType, range) ->
            SynType.SignatureParameter(List.map this.VisitSynAttributeList synAttributeLists, optional, Option.map this.VisitIdent identOption, this.VisitSynType usedType, range)

    abstract VisitSynTypeOrTrivia: SynTypeOrTrivia -> SynTypeOrTrivia
    default this.VisitSynTypeOrTrivia(synTypeOrTrivia: SynTypeOrTrivia): SynTypeOrTrivia = synTypeOrTrivia
    
    abstract VisitSynConst: SynConst -> SynConst
    default this.VisitSynConst(sc: SynConst): SynConst = sc

    abstract VisitSynValInfo: SynValInfo -> SynValInfo

    default this.VisitSynValInfo(svi: SynValInfo): SynValInfo =
        match svi with
        | SynValInfo(args, arg) ->
            SynValInfo(args |> List.map (List.map this.VisitSynArgInfo), this.VisitSynArgInfo arg)

    abstract VisitSynArgInfo: SynArgInfo -> SynArgInfo

    default this.VisitSynArgInfo(sai: SynArgInfo): SynArgInfo =
        match sai with
        | SynArgInfo(attrs, optional, ident) ->
            SynArgInfo(attrs |> List.map this.VisitSynAttributeList, optional, Option.map this.VisitIdent ident)

    abstract VisitSynTupleTypeSegment: SynTupleTypeSegment -> SynTupleTypeSegment

    default this.VisitSynTupleTypeSegment(tts: SynTupleTypeSegment): SynTupleTypeSegment =
        match tts with
        | SynTupleTypeSegment.Type(typeName) -> SynTupleTypeSegment.Type(this.VisitSynType typeName)
        | SynTupleTypeSegment.Star(range) -> SynTupleTypeSegment.Star(range)
        | SynTupleTypeSegment.Slash(range) -> SynTupleTypeSegment.Slash(range)
    
    abstract VisitSynAccess: SynAccess -> SynAccess

    default this.VisitSynAccess(a: SynAccess): SynAccess = a

    abstract VisitSynBindingKind: SynBindingKind -> SynBindingKind

    default this.VisitSynBindingKind(kind: SynBindingKind): SynBindingKind = kind

    abstract VisitParsedHashDirective: ParsedHashDirective -> ParsedHashDirective

    default this.VisitParsedHashDirective(hash: ParsedHashDirective): ParsedHashDirective =
        match hash with
        | ParsedHashDirective(ident, longIdent, range) -> ParsedHashDirective(ident, longIdent, range)

    abstract VisitSynModuleOrNamespaceSig: SynModuleOrNamespaceSig -> SynModuleOrNamespaceSig

    default this.VisitSynModuleOrNamespaceSig(modOrNs: SynModuleOrNamespaceSig): SynModuleOrNamespaceSig =
        match modOrNs with
        | SynModuleOrNamespaceSig(longIdent, isRecursive, isModule, decls, doc, attrs, access, range, trivia) ->
            SynModuleOrNamespaceSig
                (this.VisitLongIdent longIdent, isRecursive, isModule, decls |> List.map this.VisitSynModuleSigDecl, this.VisitPreXmlDoc(doc),
                 attrs |> List.map this.VisitSynAttributeList, Option.map this.VisitSynAccess access, range, trivia)

    abstract VisitSynModuleSigDecl: SynModuleSigDecl -> SynModuleSigDecl

    default this.VisitSynModuleSigDecl(ast: SynModuleSigDecl): SynModuleSigDecl =
        match ast with
        | SynModuleSigDecl.ModuleAbbrev(ident, longIdent, range) ->
            SynModuleSigDecl.ModuleAbbrev(this.VisitIdent ident, this.VisitLongIdent longIdent, range)
        | SynModuleSigDecl.NestedModule(sci, isRecursive, decls, range, trivia) ->
            SynModuleSigDecl.NestedModule
                (this.VisitSynComponentInfo sci, isRecursive, decls |> List.map this.VisitSynModuleSigDecl, range, trivia)
        | SynModuleSigDecl.Val(node, range) -> SynModuleSigDecl.Val(this.VisitSynValSig node, range)
        | SynModuleSigDecl.Types(typeDefs, range) ->
            SynModuleSigDecl.Types(typeDefs |> List.map this.VisitSynTypeDefnSig, range)
        | SynModuleSigDecl.Open(target, range) -> SynModuleSigDecl.Open(this.VisitSynOpenDeclTarget target, range)
        | SynModuleSigDecl.HashDirective(hash, range) ->
            SynModuleSigDecl.HashDirective(this.VisitParsedHashDirective hash, range)
        | SynModuleSigDecl.NamespaceFragment(moduleOrNamespace) ->
            SynModuleSigDecl.NamespaceFragment(this.VisitSynModuleOrNamespaceSig moduleOrNamespace)
        | SynModuleSigDecl.Exception(synExceptionSig, range) ->
            SynModuleSigDecl.Exception(this.VisitSynExceptionSig synExceptionSig, range)

    abstract VisitSynExceptionSig: SynExceptionSig -> SynExceptionSig

    default this.VisitSynExceptionSig(exceptionDef: SynExceptionSig): SynExceptionSig =
        match exceptionDef with
        | SynExceptionSig(sedr, withKeyword, members, range) ->
            SynExceptionSig(this.VisitSynExceptionDefnRepr sedr, withKeyword, members |> List.map this.VisitSynMemberSig, range)
            
    abstract VisitSynIdent: SynIdent -> SynIdent
    default this.VisitSynIdent(ident: SynIdent) =
        match ident with
        | SynIdent(ident, identTriviaOption) -> SynIdent(this.VisitIdent ident, identTriviaOption)

    abstract VisitLongIdent: LongIdent -> LongIdent
    default this.VisitLongIdent(li: LongIdent): LongIdent = List.map this.VisitIdent li

    abstract VisitIdent: Ident -> Ident
    default this.VisitIdent(ident: Ident): Ident = ident
    
    abstract VisitPreXmlDoc: PreXmlDoc -> PreXmlDoc
    default this.VisitPreXmlDoc(doc: PreXmlDoc): PreXmlDoc = doc
