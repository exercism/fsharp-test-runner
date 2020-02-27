module Exercism.TestRunner.FSharp.Syntax

open FSharp.Compiler.Ast
open Fantomas

let private formatConfig = FormatConfig.FormatConfig.Default

type SyntaxVisitor() =
    abstract visitInput: ParsedInput -> ParsedInput

    default this.visitInput (input: ParsedInput): ParsedInput =
        match input with
        | ParsedInput.ImplFile(ParsedImplFileInput(file, isScript, qualName, pragmas, hashDirectives, modules, b)) ->
            ParsedInput.ImplFile
                (ParsedImplFileInput
                    (file, isScript, qualName, pragmas, hashDirectives, List.map this.visitSynModuleOrNamespace modules,
                     b))
        | ParsedInput.SigFile(ParsedSigFileInput(file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs)) ->
            ParsedInput.SigFile(ParsedSigFileInput(file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs))

    abstract visitSynModuleOrNamespace: SynModuleOrNamespace -> SynModuleOrNamespace

    default this.visitSynModuleOrNamespace (modOrNs: SynModuleOrNamespace): SynModuleOrNamespace =
        match modOrNs with
        | SynModuleOrNamespace(longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            SynModuleOrNamespace
                (this.visitLongIdent longIdent, isRecursive, isModule, decls |> List.map this.visitSynModuleDecl, doc,
                 attrs |> List.map this.visitSynAttributeList, Option.map this.visitSynAccess access, range)

    abstract visitSynModuleDecl: SynModuleDecl -> SynModuleDecl

    default this.visitSynModuleDecl (synMod: SynModuleDecl): SynModuleDecl =
        match synMod with
        | SynModuleDecl.ModuleAbbrev(ident, longIdent, range) ->
            SynModuleDecl.ModuleAbbrev(this.visitIdent ident, this.visitLongIdent longIdent, range)
        | SynModuleDecl.NestedModule(sci, isRecursive, decls, b, range) ->
            SynModuleDecl.NestedModule
                (this.visitSynComponentInfo sci, isRecursive, decls |> List.map this.visitSynModuleDecl, b, range)
        | SynModuleDecl.Let(isRecursive, bindings, range) ->
            SynModuleDecl.Let(isRecursive, bindings |> List.map this.visitSynBinding, range)
        | SynModuleDecl.DoExpr(pi, expr, range) -> SynModuleDecl.DoExpr(pi, this.visitSynExpr expr, range)
        | SynModuleDecl.Types(typeDefs, range) ->
            SynModuleDecl.Types(typeDefs |> List.map this.visitSynTypeDefn, range)
        | SynModuleDecl.Exception(exceptionDef, range) ->
            SynModuleDecl.Exception(this.visitSynExceptionDefn exceptionDef, range)
        | SynModuleDecl.Open(longDotId, range) -> SynModuleDecl.Open(this.visitLongIdentWithDots longDotId, range)
        | SynModuleDecl.Attributes(attrs, range) ->
            SynModuleDecl.Attributes(attrs |> List.map this.visitSynAttributeList, range)
        | SynModuleDecl.HashDirective(hash, range) ->
            SynModuleDecl.HashDirective(this.visitParsedHashDirective hash, range)
        | SynModuleDecl.NamespaceFragment(moduleOrNamespace) ->
            SynModuleDecl.NamespaceFragment(this.visitSynModuleOrNamespace moduleOrNamespace)

    abstract visitSynExpr: SynExpr -> SynExpr

    default this.visitSynExpr (synExpr: SynExpr): SynExpr =
        match synExpr with
        | SynExpr.Paren(expr, leftParenRange, rightParenRange, range) ->
            SynExpr.Paren(this.visitSynExpr expr, leftParenRange, rightParenRange, range)
        | SynExpr.Quote(operator, isRaw, quotedSynExpr, isFromQueryExpression, range) ->
            SynExpr.Quote
                (this.visitSynExpr operator, isRaw, this.visitSynExpr quotedSynExpr, isFromQueryExpression, range)
        | SynExpr.Const(constant, range) -> SynExpr.Const(this.visitSynConst constant, range)
        | SynExpr.Typed(expr, typeName, range) ->
            SynExpr.Typed(this.visitSynExpr expr, this.visitSynType typeName, range)
        | SynExpr.Tuple(isStruct, exprs, commaRanges, range) ->
            SynExpr.Tuple(isStruct, exprs |> List.map this.visitSynExpr, commaRanges, range)
        | SynExpr.ArrayOrList(isList, exprs, range) ->
            SynExpr.ArrayOrList(isList, exprs |> List.map this.visitSynExpr, range)
        | SynExpr.Record(typInfo, copyInfo, recordFields, range) ->
            SynExpr.Record
                (typInfo
                 |> Option.map
                     (fun (typ, expr, leftParenRange, sep, rightParentRange) ->
                     (this.visitSynType typ, this.visitSynExpr expr, leftParenRange, sep, rightParentRange)),
                 copyInfo |> Option.map (fun (expr, opt) -> (this.visitSynExpr expr, opt)),
                 recordFields |> List.map this.visitRecordField, range)
        | SynExpr.AnonRecd(isStruct, copyInfo, recordFields, range) ->
            SynExpr.AnonRecd
                (isStruct, copyInfo |> Option.map (fun (expr, opt) -> (this.visitSynExpr expr, opt)),
                 recordFields |> List.map this.visitAnonRecordField, range)
        | SynExpr.New(isProtected, typeName, expr, range) ->
            SynExpr.New(isProtected, this.visitSynType typeName, this.visitSynExpr expr, range)
        | SynExpr.ObjExpr(objType, argOptions, bindings, extraImpls, newExprRange, range) ->
            SynExpr.ObjExpr
                (this.visitSynType objType, Option.map this.visitArgsOption argOptions,
                 bindings |> List.map this.visitSynBinding, extraImpls |> List.map this.visitSynInterfaceImpl,
                 newExprRange, range)
        | SynExpr.While(seqPoint, whileExpr, doExpr, range) ->
            SynExpr.While(seqPoint, this.visitSynExpr whileExpr, this.visitSynExpr doExpr, range)
        | SynExpr.For(seqPoint, ident, identBody, b, toBody, doBody, range) ->
            SynExpr.For
                (seqPoint, this.visitIdent ident, this.visitSynExpr identBody, b, this.visitSynExpr toBody,
                 this.visitSynExpr doBody, range)
        | SynExpr.ForEach(seqPoint, (SeqExprOnly seqExprOnly), isFromSource, pat, enumExpr, bodyExpr, range) ->
            SynExpr.ForEach
                (seqPoint, (SeqExprOnly seqExprOnly), isFromSource, this.visitSynPat pat, this.visitSynExpr enumExpr,
                 this.visitSynExpr bodyExpr, range)
        | SynExpr.ArrayOrListOfSeqExpr(isArray, expr, range) ->
            SynExpr.ArrayOrListOfSeqExpr(isArray, this.visitSynExpr expr, range)
        | SynExpr.CompExpr(isArrayOrList, isNotNakedRefCell, expr, range) ->
            SynExpr.CompExpr(isArrayOrList, isNotNakedRefCell, this.visitSynExpr expr, range)
        | SynExpr.Lambda(fromMethod, inLambdaSeq, args, body, range) ->
            SynExpr.Lambda(fromMethod, inLambdaSeq, this.visitSynSimplePats args, this.visitSynExpr body, range)
        | SynExpr.MatchLambda(isExnMatch, r, matchClaseus, seqPoint, range) ->
            SynExpr.MatchLambda(isExnMatch, r, matchClaseus |> List.map this.visitSynMatchClause, seqPoint, range)
        | SynExpr.Match(seqPoint, expr, clauses, range) ->
            SynExpr.Match(seqPoint, this.visitSynExpr expr, clauses |> List.map this.visitSynMatchClause, range)
        | SynExpr.Do(expr, range) -> SynExpr.Do(this.visitSynExpr expr, range)
        | SynExpr.Assert(expr, range) -> SynExpr.Assert(this.visitSynExpr expr, range)
        | SynExpr.App(atomicFlag, isInfix, funcExpr, argExpr, range) ->
            SynExpr.App(atomicFlag, isInfix, this.visitSynExpr funcExpr, this.visitSynExpr argExpr, range)
        | SynExpr.TypeApp(expr, lESSrange, typeNames, commaRanges, gREATERrange, typeArgsRange, range) ->
            SynExpr.TypeApp
                (this.visitSynExpr expr, lESSrange, typeNames |> List.map this.visitSynType, commaRanges, gREATERrange,
                 typeArgsRange, range)
        | SynExpr.LetOrUse(isRecursive, isUse, bindings, body, range) ->
            SynExpr.LetOrUse
                (isRecursive, isUse, bindings |> List.map this.visitSynBinding, this.visitSynExpr body, range)
        | SynExpr.TryWith(tryExpr, tryRange, withCases, withRange, range, trySeqPoint, withSeqPoint) ->
            SynExpr.TryWith
                (this.visitSynExpr tryExpr, tryRange, withCases |> List.map this.visitSynMatchClause, withRange, range,
                 trySeqPoint, withSeqPoint)
        | SynExpr.TryFinally(tryExpr, finallyExpr, range, trySeqPoint, withSeqPoint) ->
            SynExpr.TryFinally
                (this.visitSynExpr tryExpr, this.visitSynExpr finallyExpr, range, trySeqPoint, withSeqPoint)
        | SynExpr.Lazy(ex, range) -> SynExpr.Lazy(this.visitSynExpr ex, range)
        | SynExpr.Sequential(seqPoint, isTrueSeq, expr1, expr2, range) ->
            SynExpr.Sequential(seqPoint, isTrueSeq, this.visitSynExpr expr1, this.visitSynExpr expr2, range)
        | SynExpr.SequentialOrImplicitYield(seqPoint, expr1, expr2, ifNotStmt, range) ->
            SynExpr.SequentialOrImplicitYield
                (seqPoint, this.visitSynExpr expr1, this.visitSynExpr expr2, this.visitSynExpr ifNotStmt, range)
        | SynExpr.IfThenElse(ifExpr, thenExpr, elseExpr, seqPoint, isFromErrorRecovery, ifToThenRange, range) ->
            SynExpr.IfThenElse
                (this.visitSynExpr ifExpr, this.visitSynExpr thenExpr, Option.map this.visitSynExpr elseExpr, seqPoint,
                 isFromErrorRecovery, ifToThenRange, range)
        | SynExpr.Ident(id) -> SynExpr.Ident(id)
        | SynExpr.LongIdent(isOptional, longDotId, seqPoint, range) ->
            SynExpr.LongIdent(isOptional, this.visitLongIdentWithDots longDotId, seqPoint, range)
        | SynExpr.LongIdentSet(longDotId, expr, range) ->
            SynExpr.LongIdentSet(this.visitLongIdentWithDots longDotId, this.visitSynExpr expr, range)
        | SynExpr.DotGet(expr, rangeOfDot, longDotId, range) ->
            SynExpr.DotGet(this.visitSynExpr expr, rangeOfDot, this.visitLongIdentWithDots longDotId, range)
        | SynExpr.DotSet(expr, longDotId, e2, range) ->
            SynExpr.DotSet
                (this.visitSynExpr expr, this.visitLongIdentWithDots longDotId, this.visitSynExpr e2, range)
        | SynExpr.Set(e1, e2, range) -> SynExpr.Set(this.visitSynExpr e1, this.visitSynExpr e2, range)
        | SynExpr.DotIndexedGet(objectExpr, indexExprs, dotRange, range) ->
            SynExpr.DotIndexedGet
                (this.visitSynExpr objectExpr, indexExprs |> List.map this.visitSynIndexerArg, dotRange, range)
        | SynExpr.DotIndexedSet(objectExpr, indexExprs, valueExpr, leftOfSetRange, dotRange, range) ->
            SynExpr.DotIndexedSet
                (this.visitSynExpr objectExpr, indexExprs |> List.map this.visitSynIndexerArg,
                 this.visitSynExpr valueExpr, leftOfSetRange, dotRange, range)
        | SynExpr.NamedIndexedPropertySet(longDotId, e1, e2, range) ->
            SynExpr.NamedIndexedPropertySet
                (this.visitLongIdentWithDots longDotId, this.visitSynExpr e1, this.visitSynExpr e2, range)
        | SynExpr.DotNamedIndexedPropertySet(expr, longDotId, e1, e2, range) ->
            SynExpr.DotNamedIndexedPropertySet
                (this.visitSynExpr expr, this.visitLongIdentWithDots longDotId, this.visitSynExpr e1,
                 this.visitSynExpr e2, range)
        | SynExpr.TypeTest(expr, typeName, range) ->
            SynExpr.TypeTest(this.visitSynExpr expr, this.visitSynType typeName, range)
        | SynExpr.Upcast(expr, typeName, range) ->
            SynExpr.Upcast(this.visitSynExpr expr, this.visitSynType typeName, range)
        | SynExpr.Downcast(expr, typeName, range) ->
            SynExpr.Downcast(this.visitSynExpr expr, this.visitSynType typeName, range)
        | SynExpr.InferredUpcast(expr, range) -> SynExpr.InferredUpcast(this.visitSynExpr expr, range)
        | SynExpr.InferredDowncast(expr, range) -> SynExpr.InferredDowncast(this.visitSynExpr expr, range)
        | SynExpr.Null(range) -> SynExpr.Null(range)
        | SynExpr.AddressOf(isByref, expr, refRange, range) ->
            SynExpr.AddressOf(isByref, this.visitSynExpr expr, refRange, range)
        | SynExpr.TraitCall(typars, sign, expr, range) ->
            SynExpr.TraitCall
                (typars |> List.map this.visitSynTypar, this.visitSynMemberSig sign, this.visitSynExpr expr, range)
        | SynExpr.JoinIn(expr, inrange, expr2, range) ->
            SynExpr.JoinIn(this.visitSynExpr expr, inrange, this.visitSynExpr expr2, range)
        | SynExpr.ImplicitZero(range) -> SynExpr.ImplicitZero(range)
        | SynExpr.YieldOrReturn(info, expr, range) -> SynExpr.YieldOrReturn(info, this.visitSynExpr expr, range)
        | SynExpr.YieldOrReturnFrom(info, expr, range) ->
            SynExpr.YieldOrReturnFrom(info, this.visitSynExpr expr, range)
        | SynExpr.LetOrUseBang(seqPoint, isUse, isFromSource, pat, rhsExpr, bodyExpr, range) ->
            SynExpr.LetOrUseBang
                (seqPoint, isUse, isFromSource, this.visitSynPat pat, this.visitSynExpr rhsExpr,
                 this.visitSynExpr bodyExpr, range)
        | SynExpr.MatchBang(seqPoint, expr, clauses, range) ->
            SynExpr.MatchBang(seqPoint, this.visitSynExpr expr, clauses |> List.map this.visitSynMatchClause, range)
        | SynExpr.DoBang(expr, range) -> SynExpr.DoBang(this.visitSynExpr expr, range)
        | SynExpr.LibraryOnlyILAssembly(a, typs, exprs, typs2, range) ->
            SynExpr.LibraryOnlyILAssembly
                (a, List.map this.visitSynType typs, List.map this.visitSynExpr exprs, List.map this.visitSynType typs2,
                 range)
        | SynExpr.LibraryOnlyStaticOptimization(constraints, expr1, expr2, range) ->
            SynExpr.LibraryOnlyStaticOptimization
                (constraints, this.visitSynExpr expr1, this.visitSynExpr expr2, range)
        | SynExpr.LibraryOnlyUnionCaseFieldGet(expr, longId, i, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldGet(this.visitSynExpr expr, this.visitLongIdent longId, i, range)
        | SynExpr.LibraryOnlyUnionCaseFieldSet(e1, longId, i, e2, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldSet
                (this.visitSynExpr e1, this.visitLongIdent longId, i, this.visitSynExpr e2, range)
        | SynExpr.ArbitraryAfterError(debugStr, range) -> SynExpr.ArbitraryAfterError(debugStr, range)
        | SynExpr.FromParseError(expr, range) -> SynExpr.FromParseError(this.visitSynExpr expr, range)
        | SynExpr.DiscardAfterMissingQualificationAfterDot(expr, range) ->
            SynExpr.DiscardAfterMissingQualificationAfterDot(this.visitSynExpr expr, range)
        | SynExpr.Fixed(expr, range) -> SynExpr.Fixed(this.visitSynExpr expr, range)

    abstract visitRecordField: (RecordFieldName * SynExpr option * BlockSeparator option)
     -> RecordFieldName * SynExpr option * BlockSeparator option
    default this.visitRecordField (((longId, correct), expr: SynExpr option, sep: BlockSeparator option)) =
        ((this.visitLongIdentWithDots longId, correct), Option.map this.visitSynExpr expr, sep)

    abstract visitAnonRecordField: (Ident * SynExpr) -> Ident * SynExpr
    default this.visitAnonRecordField ((ident: Ident, expr: SynExpr)) =
        (this.visitIdent ident, this.visitSynExpr expr)

    abstract visitAnonRecordTypeField: (Ident * SynType) -> Ident * SynType
    default this.visitAnonRecordTypeField ((ident: Ident, t: SynType)) = (this.visitIdent ident, this.visitSynType t)

    abstract visitSynMemberSig: SynMemberSig -> SynMemberSig

    default this.visitSynMemberSig (ms: SynMemberSig): SynMemberSig =
        match ms with
        | SynMemberSig.Member(valSig, flags, range) -> SynMemberSig.Member(this.visitSynValSig valSig, flags, range)
        | SynMemberSig.Interface(typeName, range) -> SynMemberSig.Interface(this.visitSynType typeName, range)
        | SynMemberSig.Inherit(typeName, range) -> SynMemberSig.Inherit(this.visitSynType typeName, range)
        | SynMemberSig.ValField(f, range) -> SynMemberSig.ValField(this.visitSynField f, range)
        | SynMemberSig.NestedType(typedef, range) -> SynMemberSig.NestedType(this.visitSynTypeDefnSig typedef, range)

    abstract visitSynIndexerArg: SynIndexerArg -> SynIndexerArg

    default this.visitSynIndexerArg (ia: SynIndexerArg): SynIndexerArg =
        match ia with
        | SynIndexerArg.One(e) -> SynIndexerArg.One(this.visitSynExpr e)
        | SynIndexerArg.Two(e1, e2) -> SynIndexerArg.Two(this.visitSynExpr e1, this.visitSynExpr e2)

    abstract visitSynMatchClause: SynMatchClause -> SynMatchClause

    default this.visitSynMatchClause (mc: SynMatchClause): SynMatchClause =
        match mc with
        | SynMatchClause.Clause(pat, e1, e2, range, pi) ->
            SynMatchClause.Clause(this.visitSynPat pat, Option.map this.visitSynExpr e1, e2, range, pi)

    abstract visitArgsOption: (SynExpr * Ident option) -> SynExpr * Ident option
    default this.visitArgsOption ((expr: SynExpr, ident: Ident option)) =
        (this.visitSynExpr expr, Option.map this.visitIdent ident)

    abstract visitSynInterfaceImpl: SynInterfaceImpl -> SynInterfaceImpl

    default this.visitSynInterfaceImpl (ii: SynInterfaceImpl): SynInterfaceImpl =
        match ii with
        | InterfaceImpl(typ, bindings, range) ->
            InterfaceImpl(this.visitSynType typ, bindings |> List.map this.visitSynBinding, range)

    abstract visitSynTypeDefn: SynTypeDefn -> SynTypeDefn

    default this.visitSynTypeDefn (td: SynTypeDefn): SynTypeDefn =
        match td with
        | TypeDefn(sci, stdr, members, range) ->
            TypeDefn
                (this.visitSynComponentInfo sci, this.visitSynTypeDefnRepr stdr,
                 members |> List.map this.visitSynMemberDefn, range)

    abstract visitSynTypeDefnSig: SynTypeDefnSig -> SynTypeDefnSig

    default this.visitSynTypeDefnSig (typeDefSig: SynTypeDefnSig): SynTypeDefnSig =
        match typeDefSig with
        | TypeDefnSig(sci, synTypeDefnSigReprs, memberSig, range) ->
            TypeDefnSig
                (this.visitSynComponentInfo sci, this.visitSynTypeDefnSigRepr synTypeDefnSigReprs,
                 memberSig |> List.map this.visitSynMemberSig, range)

    abstract visitSynTypeDefnSigRepr: SynTypeDefnSigRepr -> SynTypeDefnSigRepr

    default this.visitSynTypeDefnSigRepr (stdr: SynTypeDefnSigRepr): SynTypeDefnSigRepr =
        match stdr with
        | SynTypeDefnSigRepr.ObjectModel(kind, members, range) ->
            SynTypeDefnSigRepr.ObjectModel
                (this.visitSynTypeDefnKind kind, members |> List.map this.visitSynMemberSig, range)
        | SynTypeDefnSigRepr.Simple(simpleRepr, range) ->
            SynTypeDefnSigRepr.Simple(this.visitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnSigRepr.Exception(exceptionRepr) ->
            SynTypeDefnSigRepr.Exception(this.visitSynExceptionDefnRepr exceptionRepr)

    abstract visitSynMemberDefn: SynMemberDefn -> SynMemberDefn

    default this.visitSynMemberDefn (mbrDef: SynMemberDefn): SynMemberDefn =
        match mbrDef with
        | SynMemberDefn.Open(longIdent, range) -> SynMemberDefn.Open(this.visitLongIdent longIdent, range)
        | SynMemberDefn.Member(memberDefn, range) -> SynMemberDefn.Member(this.visitSynBinding memberDefn, range)
        | SynMemberDefn.ImplicitCtor(access, attrs, ctorArgs, selfIdentifier, range) ->
            SynMemberDefn.ImplicitCtor
                (Option.map this.visitSynAccess access, attrs |> List.map this.visitSynAttributeList,
                 this.visitSynSimplePats ctorArgs, Option.map this.visitIdent selfIdentifier, range)
        | SynMemberDefn.ImplicitInherit(inheritType, inheritArgs, inheritAlias, range) ->
            SynMemberDefn.ImplicitInherit
                (this.visitSynType inheritType, this.visitSynExpr inheritArgs, Option.map this.visitIdent inheritAlias,
                 range)
        | SynMemberDefn.LetBindings(bindings, isStatic, isRecursive, range) ->
            SynMemberDefn.LetBindings(bindings |> List.map this.visitSynBinding, isStatic, isRecursive, range)
        | SynMemberDefn.AbstractSlot(valSig, flags, range) ->
            SynMemberDefn.AbstractSlot(this.visitSynValSig valSig, flags, range)
        | SynMemberDefn.Interface(typ, members, range) ->
            SynMemberDefn.Interface
                (this.visitSynType typ, Option.map (List.map this.visitSynMemberDefn) members, range)
        | SynMemberDefn.Inherit(typ, ident, range) ->
            SynMemberDefn.Inherit(this.visitSynType typ, Option.map this.visitIdent ident, range)
        | SynMemberDefn.ValField(fld, range) -> SynMemberDefn.ValField(this.visitSynField fld, range)
        | SynMemberDefn.NestedType(typeDefn, access, range) ->
            SynMemberDefn.NestedType(this.visitSynTypeDefn typeDefn, Option.map this.visitSynAccess access, range)
        | SynMemberDefn.AutoProperty(attrs, isStatic, ident, typeOpt, propKind, flags, doc, access, synExpr, getSetRange,
                                     range) ->
            SynMemberDefn.AutoProperty
                (attrs |> List.map this.visitSynAttributeList, isStatic, this.visitIdent ident,
                 Option.map this.visitSynType typeOpt, propKind, flags, doc, Option.map this.visitSynAccess access,
                 this.visitSynExpr synExpr, getSetRange, range)

    abstract visitSynSimplePat: SynSimplePat -> SynSimplePat

    default this.visitSynSimplePat (sp: SynSimplePat): SynSimplePat =
        match sp with
        | SynSimplePat.Id(ident, altName, isCompilerGenerated, isThisVar, isOptArg, range) ->
            SynSimplePat.Id(this.visitIdent ident, altName, isCompilerGenerated, isThisVar, isOptArg, range)
        | SynSimplePat.Typed(simplePat, typ, range) ->
            SynSimplePat.Typed(this.visitSynSimplePat simplePat, this.visitSynType typ, range)
        | SynSimplePat.Attrib(simplePat, attrs, range) ->
            SynSimplePat.Attrib(this.visitSynSimplePat simplePat, attrs |> List.map this.visitSynAttributeList, range)

    abstract visitSynSimplePats: SynSimplePats -> SynSimplePats

    default this.visitSynSimplePats (sp: SynSimplePats): SynSimplePats =
        match sp with
        | SynSimplePats.SimplePats(pats, range) ->
            SynSimplePats.SimplePats(pats |> List.map this.visitSynSimplePat, range)
        | SynSimplePats.Typed(pats, typ, range) ->
            SynSimplePats.Typed(this.visitSynSimplePats pats, this.visitSynType typ, range)

    abstract visitSynBinding: SynBinding -> SynBinding

    default this.visitSynBinding (binding: SynBinding): SynBinding =
        match binding with
        | Binding(access, kind, mustInline, isMutable, attrs, doc, valData, headPat, returnInfo, expr, range, seqPoint) ->
            Binding
                (Option.map this.visitSynAccess access, kind, mustInline, isMutable,
                 attrs |> List.map this.visitSynAttributeList, doc, this.visitSynValData valData,
                 this.visitSynPat headPat, Option.map this.visitSynBindingReturnInfo returnInfo, this.visitSynExpr expr,
                 range, seqPoint)

    abstract visitSynValData: SynValData -> SynValData

    default this.visitSynValData (svd: SynValData): SynValData =
        match svd with
        | SynValData(flags, svi, ident) ->
            SynValData(flags, this.visitSynValInfo svi, Option.map this.visitIdent ident)

    abstract visitSynValSig: SynValSig -> SynValSig

    default this.visitSynValSig (svs: SynValSig): SynValSig =
        match svs with
        | ValSpfn(attrs, ident, explicitValDecls, synType, arity, isInline, isMutable, doc, access, expr, range) ->
            ValSpfn
                (attrs |> List.map this.visitSynAttributeList, this.visitIdent ident,
                 this.visitSynValTyparDecls explicitValDecls, this.visitSynType synType, this.visitSynValInfo arity,
                 isInline, isMutable, doc, Option.map this.visitSynAccess access, Option.map this.visitSynExpr expr,
                 range)

    abstract visitSynValTyparDecls: SynValTyparDecls -> SynValTyparDecls

    default this.visitSynValTyparDecls (valTypeDecl: SynValTyparDecls): SynValTyparDecls =
        match valTypeDecl with
        | SynValTyparDecls(typardecls, b, constraints) ->
            SynValTyparDecls(typardecls |> List.map this.visitSynTyparDecl, b, constraints)

    abstract visitSynTyparDecl: SynTyparDecl -> SynTyparDecl

    default this.visitSynTyparDecl (std: SynTyparDecl): SynTyparDecl =
        match std with
        | TyparDecl(attrs, typar) -> TyparDecl(attrs |> List.map this.visitSynAttributeList, this.visitSynTypar typar)

    abstract visitSynTypar: SynTypar -> SynTypar

    default this.visitSynTypar (typar: SynTypar): SynTypar =
        match typar with
        | Typar(ident, staticReq, isComGen) -> Typar(this.visitIdent ident, staticReq, isComGen)

    abstract visitTyparStaticReq: TyparStaticReq -> TyparStaticReq

    default this.visitTyparStaticReq (tsr: TyparStaticReq): TyparStaticReq =
        match tsr with
        | NoStaticReq -> tsr
        | HeadTypeStaticReq -> tsr

    abstract visitSynBindingReturnInfo: SynBindingReturnInfo -> SynBindingReturnInfo

    default this.visitSynBindingReturnInfo (returnInfo: SynBindingReturnInfo): SynBindingReturnInfo =
        match returnInfo with
        | SynBindingReturnInfo(typeName, range, attrs) ->
            SynBindingReturnInfo(this.visitSynType typeName, range, attrs |> List.map this.visitSynAttributeList)

    abstract visitSynPat: SynPat -> SynPat

    default this.visitSynPat (sp: SynPat): SynPat =
        match sp with
        | SynPat.Const(sc, range) -> SynPat.Const(this.visitSynConst sc, range)
        | SynPat.Wild(range) -> SynPat.Wild(range)
        | SynPat.Named(synPat, ident, isSelfIdentifier, access, range) ->
            SynPat.Named
                (this.visitSynPat synPat, this.visitIdent ident, isSelfIdentifier, Option.map this.visitSynAccess access,
                 range)
        | SynPat.Typed(synPat, synType, range) ->
            SynPat.Typed(this.visitSynPat synPat, this.visitSynType synType, range)
        | SynPat.Attrib(synPat, attrs, range) ->
            SynPat.Attrib(this.visitSynPat synPat, attrs |> List.map this.visitSynAttributeList, range)
        | SynPat.Or(synPat, synPat2, range) -> SynPat.Or(this.visitSynPat synPat, this.visitSynPat synPat2, range)
        | SynPat.Ands(pats, range) -> SynPat.Ands(pats |> List.map this.visitSynPat, range)
        | SynPat.LongIdent(longDotId, ident, svtd, ctorArgs, access, range) ->
            SynPat.LongIdent
                (this.visitLongIdentWithDots longDotId, Option.map this.visitIdent ident,
                 Option.map this.visitSynValTyparDecls svtd, this.visitSynConstructorArgs ctorArgs,
                 Option.map this.visitSynAccess access, range)
        | SynPat.Tuple(isStruct, pats, range) -> SynPat.Tuple(isStruct, pats |> List.map this.visitSynPat, range)
        | SynPat.Paren(pat, range) -> SynPat.Paren(this.visitSynPat pat, range)
        | SynPat.ArrayOrList(isList, pats, range) ->
            SynPat.ArrayOrList(isList, pats |> List.map this.visitSynPat, range)
        | SynPat.Record(pats, range) ->
            SynPat.Record
                (pats
                 |> List.map
                     (fun ((longIdent, ident), pat) ->
                     ((this.visitLongIdent longIdent, this.visitIdent ident), this.visitSynPat pat)), range)
        | SynPat.Null(range) -> SynPat.Null(range)
        | SynPat.OptionalVal(ident, range) -> SynPat.OptionalVal(this.visitIdent ident, range)
        | SynPat.IsInst(typ, range) -> SynPat.IsInst(this.visitSynType typ, range)
        | SynPat.QuoteExpr(expr, range) -> SynPat.QuoteExpr(this.visitSynExpr expr, range)
        | SynPat.DeprecatedCharRange(c, c2, range) -> SynPat.DeprecatedCharRange(c, c2, range)
        | SynPat.InstanceMember(ident, ident2, ident3, access, range) ->
            SynPat.InstanceMember
                (this.visitIdent ident, this.visitIdent ident2, Option.map this.visitIdent ident3,
                 Option.map this.visitSynAccess access, range)
        | SynPat.FromParseError(pat, range) -> SynPat.FromParseError(this.visitSynPat pat, range)

    abstract visitSynConstructorArgs: SynConstructorArgs -> SynConstructorArgs

    default this.visitSynConstructorArgs (ctorArgs: SynConstructorArgs): SynConstructorArgs =
        match ctorArgs with
        | Pats(pats) -> Pats(pats |> List.map this.visitSynPat)
        | NamePatPairs(pats, range) ->
            NamePatPairs(pats |> List.map (fun (ident, pat) -> (this.visitIdent ident, this.visitSynPat pat)), range)

    abstract visitSynComponentInfo: SynComponentInfo -> SynComponentInfo

    default this.visitSynComponentInfo (sci: SynComponentInfo): SynComponentInfo =
        match sci with
        | ComponentInfo(attribs, typeParams, constraints, longId, doc, preferPostfix, access, range) ->
            ComponentInfo
                (attribs |> List.map this.visitSynAttributeList, typeParams |> List.map (this.visitSynTyparDecl),
                 constraints, longId, doc, preferPostfix, Option.map this.visitSynAccess access, range)

    abstract visitSynTypeDefnRepr: SynTypeDefnRepr -> SynTypeDefnRepr

    default this.visitSynTypeDefnRepr (stdr: SynTypeDefnRepr): SynTypeDefnRepr =
        match stdr with
        | SynTypeDefnRepr.ObjectModel(kind, members, range) ->
            SynTypeDefnRepr.ObjectModel
                (this.visitSynTypeDefnKind kind, members |> List.map this.visitSynMemberDefn, range)
        | SynTypeDefnRepr.Simple(simpleRepr, range) ->
            SynTypeDefnRepr.Simple(this.visitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnRepr.Exception(exceptionRepr) ->
            SynTypeDefnRepr.Exception(this.visitSynExceptionDefnRepr exceptionRepr)

    abstract visitSynTypeDefnKind: SynTypeDefnKind -> SynTypeDefnKind

    default this.visitSynTypeDefnKind (kind: SynTypeDefnKind): SynTypeDefnKind =
        match kind with
        | TyconUnspecified -> TyconUnspecified
        | TyconClass -> TyconClass
        | TyconInterface -> TyconInterface
        | TyconStruct -> TyconStruct
        | TyconRecord -> TyconRecord
        | TyconUnion -> TyconUnion
        | TyconAbbrev -> TyconAbbrev
        | TyconHiddenRepr -> TyconHiddenRepr
        | TyconAugmentation -> TyconAugmentation
        | TyconILAssemblyCode -> TyconILAssemblyCode
        | TyconDelegate(typ, valinfo) -> TyconDelegate(this.visitSynType typ, this.visitSynValInfo valinfo)

    abstract visitSynTypeDefnSimpleRepr: SynTypeDefnSimpleRepr -> SynTypeDefnSimpleRepr

    default this.visitSynTypeDefnSimpleRepr (arg: SynTypeDefnSimpleRepr): SynTypeDefnSimpleRepr =
        match arg with
        | SynTypeDefnSimpleRepr.None(range) -> SynTypeDefnSimpleRepr.None(range)
        | SynTypeDefnSimpleRepr.Union(access, unionCases, range) ->
            SynTypeDefnSimpleRepr.Union
                (Option.map this.visitSynAccess access, unionCases |> List.map this.visitSynUnionCase, range)
        | SynTypeDefnSimpleRepr.Enum(enumCases, range) ->
            SynTypeDefnSimpleRepr.Enum(enumCases |> List.map this.visitSynEnumCase, range)
        | SynTypeDefnSimpleRepr.Record(access, recordFields, range) ->
            SynTypeDefnSimpleRepr.Record
                (Option.map this.visitSynAccess access, recordFields |> List.map this.visitSynField, range)
        | SynTypeDefnSimpleRepr.General(typeDefKind, a, b, c, d, e, pats, range) ->
            SynTypeDefnSimpleRepr.General(this.visitSynTypeDefnKind typeDefKind, a, b, c, d, e, pats, range) // TODO
        | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(ilType, range) ->
            SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(ilType, range)
        | SynTypeDefnSimpleRepr.TypeAbbrev(parserDetail, typ, range) ->
            SynTypeDefnSimpleRepr.TypeAbbrev(parserDetail, this.visitSynType typ, range)
        | SynTypeDefnSimpleRepr.Exception(edr) -> SynTypeDefnSimpleRepr.Exception(this.visitSynExceptionDefnRepr edr)

    abstract visitSynExceptionDefn: SynExceptionDefn -> SynExceptionDefn

    default this.visitSynExceptionDefn (exceptionDef: SynExceptionDefn): SynExceptionDefn =
        match exceptionDef with
        | SynExceptionDefn(sedr, members, range) ->
            SynExceptionDefn(this.visitSynExceptionDefnRepr sedr, members |> List.map this.visitSynMemberDefn, range)

    abstract visitSynExceptionDefnRepr: SynExceptionDefnRepr -> SynExceptionDefnRepr

    default this.visitSynExceptionDefnRepr (sedr: SynExceptionDefnRepr): SynExceptionDefnRepr =
        match sedr with
        | SynExceptionDefnRepr(attrs, unionCase, longId, doc, access, range) ->
            SynExceptionDefnRepr
                (attrs |> List.map this.visitSynAttributeList, this.visitSynUnionCase unionCase, longId, doc,
                 Option.map this.visitSynAccess access, range)

    abstract visitSynAttribute: SynAttribute -> SynAttribute

    default this.visitSynAttribute (attr: SynAttribute): SynAttribute =
        { attr with
              ArgExpr = this.visitSynExpr attr.ArgExpr
              Target = Option.map this.visitIdent attr.Target }

    abstract visitSynAttributeList: SynAttributeList -> SynAttributeList
    default this.visitSynAttributeList (attrs: SynAttributeList): SynAttributeList =
        { attrs with Attributes = attrs.Attributes |> List.map this.visitSynAttribute }

    abstract visitSynUnionCase: SynUnionCase -> SynUnionCase

    default this.visitSynUnionCase (uc: SynUnionCase): SynUnionCase =
        match uc with
        | UnionCase(attrs, ident, uct, doc, access, range) ->
            UnionCase
                (attrs |> List.map this.visitSynAttributeList, this.visitIdent ident, this.visitSynUnionCaseType uct,
                 doc, Option.map this.visitSynAccess access, range)

    abstract visitSynUnionCaseType: SynUnionCaseType -> SynUnionCaseType

    default this.visitSynUnionCaseType (uct: SynUnionCaseType): SynUnionCaseType =
        match uct with
        | UnionCaseFields(cases) -> UnionCaseFields(cases |> List.map this.visitSynField)
        | UnionCaseFullType(stype, valInfo) ->
            UnionCaseFullType(this.visitSynType stype, this.visitSynValInfo valInfo)

    abstract visitSynEnumCase: SynEnumCase -> SynEnumCase

    default this.visitSynEnumCase (sec: SynEnumCase): SynEnumCase =
        match sec with
        | EnumCase(attrs, ident, cnst, doc, range) ->
            EnumCase
                (attrs |> List.map this.visitSynAttributeList, this.visitIdent ident, this.visitSynConst cnst, doc,
                 range)

    abstract visitSynField: SynField -> SynField

    default this.visitSynField (sfield: SynField): SynField =
        match sfield with
        | Field(attrs, isStatic, ident, typ, isMutable, doc, access, range) ->
            Field
                (attrs |> List.map this.visitSynAttributeList, isStatic, Option.map this.visitIdent ident,
                 this.visitSynType typ, isMutable, doc, Option.map this.visitSynAccess access, range)

    abstract visitSynType: SynType -> SynType

    default this.visitSynType (st: SynType): SynType =
        match st with
        | SynType.LongIdent(li) -> SynType.LongIdent(li)
        | SynType.App(typeName, lessRange, typeArgs, commaRanges, greaterRange, isPostfix, range) ->
            SynType.App
                (this.visitSynType typeName, lessRange, typeArgs |> List.map this.visitSynType, commaRanges,
                 greaterRange, isPostfix, range)
        | SynType.LongIdentApp(typeName, longDotId, lessRange, typeArgs, commaRanges, greaterRange, range) ->
            SynType.LongIdentApp
                (this.visitSynType typeName, longDotId, lessRange, typeArgs |> List.map this.visitSynType, commaRanges,
                 greaterRange, range)
        | SynType.Tuple(isStruct, typeNames, range) ->
            SynType.Tuple(isStruct, typeNames |> List.map (fun (b, typ) -> (b, this.visitSynType typ)), range)
        | SynType.Array(i, elementType, range) -> SynType.Array(i, this.visitSynType elementType, range)
        | SynType.Fun(argType, returnType, range) ->
            SynType.Fun(this.visitSynType argType, this.visitSynType returnType, range)
        | SynType.Var(genericName, range) -> SynType.Var(this.visitSynTypar genericName, range)
        | SynType.Anon(range) -> SynType.Anon(range)
        | SynType.WithGlobalConstraints(typeName, constraints, range) ->
            SynType.WithGlobalConstraints(this.visitSynType typeName, constraints, range)
        | SynType.HashConstraint(synType, range) -> SynType.HashConstraint(this.visitSynType synType, range)
        | SynType.MeasureDivide(dividendType, divisorType, range) ->
            SynType.MeasureDivide(this.visitSynType dividendType, this.visitSynType divisorType, range)
        | SynType.MeasurePower(measureType, cnst, range) ->
            SynType.MeasurePower(this.visitSynType measureType, cnst, range)
        | SynType.StaticConstant(constant, range) -> SynType.StaticConstant(this.visitSynConst constant, range)
        | SynType.StaticConstantExpr(expr, range) -> SynType.StaticConstantExpr(this.visitSynExpr expr, range)
        | SynType.StaticConstantNamed(expr, typ, range) ->
            SynType.StaticConstantNamed(this.visitSynType expr, this.visitSynType typ, range)
        | SynType.AnonRecd(isStruct, typeNames, range) ->
            SynType.AnonRecd(isStruct, List.map this.visitAnonRecordTypeField typeNames, range)

    abstract visitSynConst: SynConst -> SynConst
    default this.visitSynConst (sc: SynConst): SynConst = sc

    abstract visitSynValInfo: SynValInfo -> SynValInfo

    default this.visitSynValInfo (svi: SynValInfo): SynValInfo =
        match svi with
        | SynValInfo(args, arg) ->
            SynValInfo(args |> List.map (List.map this.visitSynArgInfo), this.visitSynArgInfo arg)

    abstract visitSynArgInfo: SynArgInfo -> SynArgInfo

    default this.visitSynArgInfo (sai: SynArgInfo): SynArgInfo =
        match sai with
        | SynArgInfo(attrs, optional, ident) ->
            SynArgInfo(attrs |> List.map this.visitSynAttributeList, optional, Option.map this.visitIdent ident)

    abstract visitSynAccess: SynAccess -> SynAccess

    default this.visitSynAccess (a: SynAccess): SynAccess =
        match a with
        | SynAccess.Private -> a
        | SynAccess.Internal -> a
        | SynAccess.Public -> a

    abstract visitSynBindingKind: SynBindingKind -> SynBindingKind

    default this.visitSynBindingKind (kind: SynBindingKind): SynBindingKind =
        match kind with
        | SynBindingKind.DoBinding -> kind
        | SynBindingKind.StandaloneExpression -> kind
        | SynBindingKind.NormalBinding -> kind

    abstract visitMemberKind: MemberKind -> MemberKind

    default this.visitMemberKind (mk: MemberKind): MemberKind =
        match mk with
        | MemberKind.ClassConstructor -> mk
        | MemberKind.Constructor -> mk
        | MemberKind.Member -> mk
        | MemberKind.PropertyGet -> mk
        | MemberKind.PropertySet -> mk
        | MemberKind.PropertyGetSet -> mk

    abstract visitParsedHashDirective: ParsedHashDirective -> ParsedHashDirective

    default this.visitParsedHashDirective (hash: ParsedHashDirective): ParsedHashDirective =
        match hash with
        | ParsedHashDirective(ident, longIdent, range) -> ParsedHashDirective(ident, longIdent, range)

    abstract visitSynModuleOrNamespaceSig: SynModuleOrNamespaceSig -> SynModuleOrNamespaceSig

    default this.visitSynModuleOrNamespaceSig (modOrNs: SynModuleOrNamespaceSig): SynModuleOrNamespaceSig =
        match modOrNs with
        | SynModuleOrNamespaceSig(longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            SynModuleOrNamespaceSig
                (this.visitLongIdent longIdent, isRecursive, isModule, decls |> List.map this.visitSynModuleSigDecl, doc,
                 attrs |> List.map this.visitSynAttributeList, Option.map this.visitSynAccess access, range)

    abstract visitSynModuleSigDecl: SynModuleSigDecl -> SynModuleSigDecl

    default this.visitSynModuleSigDecl (ast: SynModuleSigDecl): SynModuleSigDecl =
        match ast with
        | SynModuleSigDecl.ModuleAbbrev(ident, longIdent, range) ->
            SynModuleSigDecl.ModuleAbbrev(this.visitIdent ident, this.visitLongIdent longIdent, range)
        | SynModuleSigDecl.NestedModule(sci, isRecursive, decls, range) ->
            SynModuleSigDecl.NestedModule
                (this.visitSynComponentInfo sci, isRecursive, decls |> List.map this.visitSynModuleSigDecl, range)
        | SynModuleSigDecl.Val(node, range) -> SynModuleSigDecl.Val(this.visitSynValSig node, range)
        | SynModuleSigDecl.Types(typeDefs, range) ->
            SynModuleSigDecl.Types(typeDefs |> List.map this.visitSynTypeDefnSig, range)
        | SynModuleSigDecl.Open(longId, range) -> SynModuleSigDecl.Open(this.visitLongIdent longId, range)
        | SynModuleSigDecl.HashDirective(hash, range) ->
            SynModuleSigDecl.HashDirective(this.visitParsedHashDirective hash, range)
        | SynModuleSigDecl.NamespaceFragment(moduleOrNamespace) ->
            SynModuleSigDecl.NamespaceFragment(this.visitSynModuleOrNamespaceSig moduleOrNamespace)
        | SynModuleSigDecl.Exception(synExceptionSig, range) ->
            SynModuleSigDecl.Exception(this.visitSynExceptionSig synExceptionSig, range)

    abstract visitSynExceptionSig: SynExceptionSig -> SynExceptionSig

    default this.visitSynExceptionSig (exceptionDef: SynExceptionSig): SynExceptionSig =
        match exceptionDef with
        | SynExceptionSig(sedr, members, range) ->
            SynExceptionSig(this.visitSynExceptionDefnRepr sedr, members |> List.map this.visitSynMemberSig, range)

    abstract visitLongIdentWithDots: LongIdentWithDots -> LongIdentWithDots

    default this.visitLongIdentWithDots (lid: LongIdentWithDots): LongIdentWithDots =
        match lid with
        | LongIdentWithDots(ids, ranges) -> LongIdentWithDots(List.map this.visitIdent ids, ranges)

    abstract visitLongIdent: LongIdent -> LongIdent
    default this.visitLongIdent (li: LongIdent): LongIdent = List.map this.visitIdent li

    abstract visitIdent: Ident -> Ident
    default this.visitIdent (ident: Ident): Ident = ident

let treeToCode tree = CodeFormatter.FormatASTAsync(tree, "", [], None, formatConfig) |> Async.RunSynchronously
