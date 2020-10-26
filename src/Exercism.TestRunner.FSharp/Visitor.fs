module Exercism.TestRunner.FSharp.Visitor

open FSharp.Compiler.Ast

type SyntaxVisitor() =
    abstract VisitInput: ParsedInput -> ParsedInput

    default this.VisitInput(input: ParsedInput): ParsedInput =
        match input with
        | ParsedInput.ImplFile (ParsedImplFileInput (file, isScript, qualName, pragmas, hashDirectives, modules, b)) ->
            ParsedInput.ImplFile
                (ParsedImplFileInput
                    (file,
                     isScript,
                     qualName,
                     pragmas,
                     hashDirectives,
                     List.map this.VisitSynModuleOrNamespace modules,
                     b))
        | ParsedInput.SigFile (ParsedSigFileInput (file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs)) ->
            ParsedInput.SigFile(ParsedSigFileInput(file, qualifiedName, pragmas, directives, synModuleOrNamespaceSigs))

    abstract VisitSynModuleOrNamespace: SynModuleOrNamespace -> SynModuleOrNamespace

    default this.VisitSynModuleOrNamespace(modOrNs: SynModuleOrNamespace): SynModuleOrNamespace =
        match modOrNs with
        | SynModuleOrNamespace (longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            SynModuleOrNamespace
                (this.VisitLongIdent longIdent,
                 isRecursive,
                 isModule,
                 decls |> List.map this.VisitSynModuleDecl,
                 doc,
                 attrs |> List.map this.VisitSynAttributeList,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynModuleDecl: SynModuleDecl -> SynModuleDecl

    default this.VisitSynModuleDecl(synMod: SynModuleDecl): SynModuleDecl =
        match synMod with
        | SynModuleDecl.ModuleAbbrev (ident, longIdent, range) ->
            SynModuleDecl.ModuleAbbrev(this.VisitIdent ident, this.VisitLongIdent longIdent, range)
        | SynModuleDecl.NestedModule (sci, isRecursive, decls, b, range) ->
            SynModuleDecl.NestedModule
                (this.VisitSynComponentInfo sci, isRecursive, decls |> List.map this.VisitSynModuleDecl, b, range)
        | SynModuleDecl.Let (isRecursive, bindings, range) ->
            SynModuleDecl.Let(isRecursive, bindings |> List.map this.VisitSynBinding, range)
        | SynModuleDecl.DoExpr (pi, expr, range) -> SynModuleDecl.DoExpr(pi, this.VisitSynExpr expr, range)
        | SynModuleDecl.Types (typeDefs, range) ->
            SynModuleDecl.Types(typeDefs |> List.map this.VisitSynTypeDefn, range)
        | SynModuleDecl.Exception (exceptionDef, range) ->
            SynModuleDecl.Exception(this.VisitSynExceptionDefn exceptionDef, range)
        | SynModuleDecl.Open (longDotId, range) -> SynModuleDecl.Open(this.VisitLongIdentWithDots longDotId, range)
        | SynModuleDecl.Attributes (attrs, range) ->
            SynModuleDecl.Attributes(attrs |> List.map this.VisitSynAttributeList, range)
        | SynModuleDecl.HashDirective (hash, range) ->
            SynModuleDecl.HashDirective(this.VisitParsedHashDirective hash, range)
        | SynModuleDecl.NamespaceFragment (moduleOrNamespace) ->
            SynModuleDecl.NamespaceFragment(this.VisitSynModuleOrNamespace moduleOrNamespace)

    abstract VisitSynExpr: SynExpr -> SynExpr

    default this.VisitSynExpr(synExpr: SynExpr): SynExpr =
        match synExpr with
        | SynExpr.Paren (expr, leftParenRange, rightParenRange, range) ->
            SynExpr.Paren(this.VisitSynExpr expr, leftParenRange, rightParenRange, range)
        | SynExpr.Quote (operator, isRaw, quotedSynExpr, isFromQueryExpression, range) ->
            SynExpr.Quote
                (this.VisitSynExpr operator, isRaw, this.VisitSynExpr quotedSynExpr, isFromQueryExpression, range)
        | SynExpr.Const (constant, range) -> SynExpr.Const(this.VisitSynConst constant, range)
        | SynExpr.Typed (expr, typeName, range) ->
            SynExpr.Typed(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Tuple (isStruct, exprs, commaRanges, range) ->
            SynExpr.Tuple(isStruct, exprs |> List.map this.VisitSynExpr, commaRanges, range)
        | SynExpr.ArrayOrList (isList, exprs, range) ->
            SynExpr.ArrayOrList(isList, exprs |> List.map this.VisitSynExpr, range)
        | SynExpr.Record (typInfo, copyInfo, recordFields, range) ->
            SynExpr.Record
                (typInfo
                 |> Option.map (fun (typ, expr, leftParenRange, sep, rightParentRange) ->
                     (this.VisitSynType typ, this.VisitSynExpr expr, leftParenRange, sep, rightParentRange)),
                 copyInfo
                 |> Option.map (fun (expr, opt) -> (this.VisitSynExpr expr, opt)),
                 recordFields |> List.map this.VisitRecordField,
                 range)
        | SynExpr.AnonRecd (isStruct, copyInfo, recordFields, range) ->
            SynExpr.AnonRecd
                (isStruct,
                 copyInfo
                 |> Option.map (fun (expr, opt) -> (this.VisitSynExpr expr, opt)),
                 recordFields |> List.map this.VisitAnonRecordField,
                 range)
        | SynExpr.New (isProtected, typeName, expr, range) ->
            SynExpr.New(isProtected, this.VisitSynType typeName, this.VisitSynExpr expr, range)
        | SynExpr.ObjExpr (objType, argOptions, bindings, extraImpls, newExprRange, range) ->
            SynExpr.ObjExpr
                (this.VisitSynType objType,
                 Option.map this.VisitArgsOption argOptions,
                 bindings |> List.map this.VisitSynBinding,
                 extraImpls |> List.map this.VisitSynInterfaceImpl,
                 newExprRange,
                 range)
        | SynExpr.While (seqPoint, whileExpr, doExpr, range) ->
            SynExpr.While(seqPoint, this.VisitSynExpr whileExpr, this.VisitSynExpr doExpr, range)
        | SynExpr.For (seqPoint, ident, identBody, b, toBody, doBody, range) ->
            SynExpr.For
                (seqPoint,
                 this.VisitIdent ident,
                 this.VisitSynExpr identBody,
                 b,
                 this.VisitSynExpr toBody,
                 this.VisitSynExpr doBody,
                 range)
        | SynExpr.ForEach (seqPoint, (SeqExprOnly seqExprOnly), isFromSource, pat, enumExpr, bodyExpr, range) ->
            SynExpr.ForEach
                (seqPoint,
                 (SeqExprOnly seqExprOnly),
                 isFromSource,
                 this.VisitSynPat pat,
                 this.VisitSynExpr enumExpr,
                 this.VisitSynExpr bodyExpr,
                 range)
        | SynExpr.ArrayOrListOfSeqExpr (isArray, expr, range) ->
            SynExpr.ArrayOrListOfSeqExpr(isArray, this.VisitSynExpr expr, range)
        | SynExpr.CompExpr (isArrayOrList, isNotNakedRefCell, expr, range) ->
            SynExpr.CompExpr(isArrayOrList, isNotNakedRefCell, this.VisitSynExpr expr, range)
        | SynExpr.Lambda (fromMethod, inLambdaSeq, args, body, range) ->
            SynExpr.Lambda(fromMethod, inLambdaSeq, this.VisitSynSimplePats args, this.VisitSynExpr body, range)
        | SynExpr.MatchLambda (isExnMatch, r, matchClaseus, seqPoint, range) ->
            SynExpr.MatchLambda(isExnMatch, r, matchClaseus |> List.map this.VisitSynMatchClause, seqPoint, range)
        | SynExpr.Match (seqPoint, expr, clauses, range) ->
            SynExpr.Match(seqPoint, this.VisitSynExpr expr, clauses |> List.map this.VisitSynMatchClause, range)
        | SynExpr.Do (expr, range) -> SynExpr.Do(this.VisitSynExpr expr, range)
        | SynExpr.Assert (expr, range) -> SynExpr.Assert(this.VisitSynExpr expr, range)
        | SynExpr.App (atomicFlag, isInfix, funcExpr, argExpr, range) ->
            SynExpr.App(atomicFlag, isInfix, this.VisitSynExpr funcExpr, this.VisitSynExpr argExpr, range)
        | SynExpr.TypeApp (expr, lESSrange, typeNames, commaRanges, gREATERrange, typeArgsRange, range) ->
            SynExpr.TypeApp
                (this.VisitSynExpr expr,
                 lESSrange,
                 typeNames |> List.map this.VisitSynType,
                 commaRanges,
                 gREATERrange,
                 typeArgsRange,
                 range)
        | SynExpr.LetOrUse (isRecursive, isUse, bindings, body, range) ->
            SynExpr.LetOrUse
                (isRecursive, isUse, bindings |> List.map this.VisitSynBinding, this.VisitSynExpr body, range)
        | SynExpr.TryWith (tryExpr, tryRange, withCases, withRange, range, trySeqPoint, withSeqPoint) ->
            SynExpr.TryWith
                (this.VisitSynExpr tryExpr,
                 tryRange,
                 withCases |> List.map this.VisitSynMatchClause,
                 withRange,
                 range,
                 trySeqPoint,
                 withSeqPoint)
        | SynExpr.TryFinally (tryExpr, finallyExpr, range, trySeqPoint, withSeqPoint) ->
            SynExpr.TryFinally
                (this.VisitSynExpr tryExpr, this.VisitSynExpr finallyExpr, range, trySeqPoint, withSeqPoint)
        | SynExpr.Lazy (ex, range) -> SynExpr.Lazy(this.VisitSynExpr ex, range)
        | SynExpr.Sequential (seqPoint, isTrueSeq, expr1, expr2, range) ->
            SynExpr.Sequential(seqPoint, isTrueSeq, this.VisitSynExpr expr1, this.VisitSynExpr expr2, range)
        | SynExpr.SequentialOrImplicitYield (seqPoint, expr1, expr2, ifNotStmt, range) ->
            SynExpr.SequentialOrImplicitYield
                (seqPoint, this.VisitSynExpr expr1, this.VisitSynExpr expr2, this.VisitSynExpr ifNotStmt, range)
        | SynExpr.IfThenElse (ifExpr, thenExpr, elseExpr, seqPoint, isFromErrorRecovery, ifToThenRange, range) ->
            SynExpr.IfThenElse
                (this.VisitSynExpr ifExpr,
                 this.VisitSynExpr thenExpr,
                 Option.map this.VisitSynExpr elseExpr,
                 seqPoint,
                 isFromErrorRecovery,
                 ifToThenRange,
                 range)
        | SynExpr.Ident (id) -> SynExpr.Ident(id)
        | SynExpr.LongIdent (isOptional, longDotId, seqPoint, range) ->
            SynExpr.LongIdent(isOptional, this.VisitLongIdentWithDots longDotId, seqPoint, range)
        | SynExpr.LongIdentSet (longDotId, expr, range) ->
            SynExpr.LongIdentSet(this.VisitLongIdentWithDots longDotId, this.VisitSynExpr expr, range)
        | SynExpr.DotGet (expr, rangeOfDot, longDotId, range) ->
            SynExpr.DotGet(this.VisitSynExpr expr, rangeOfDot, this.VisitLongIdentWithDots longDotId, range)
        | SynExpr.DotSet (expr, longDotId, e2, range) ->
            SynExpr.DotSet(this.VisitSynExpr expr, this.VisitLongIdentWithDots longDotId, this.VisitSynExpr e2, range)
        | SynExpr.Set (e1, e2, range) -> SynExpr.Set(this.VisitSynExpr e1, this.VisitSynExpr e2, range)
        | SynExpr.DotIndexedGet (objectExpr, indexExprs, dotRange, range) ->
            SynExpr.DotIndexedGet
                (this.VisitSynExpr objectExpr, indexExprs |> List.map this.VisitSynIndexerArg, dotRange, range)
        | SynExpr.DotIndexedSet (objectExpr, indexExprs, valueExpr, leftOfSetRange, dotRange, range) ->
            SynExpr.DotIndexedSet
                (this.VisitSynExpr objectExpr,
                 indexExprs |> List.map this.VisitSynIndexerArg,
                 this.VisitSynExpr valueExpr,
                 leftOfSetRange,
                 dotRange,
                 range)
        | SynExpr.NamedIndexedPropertySet (longDotId, e1, e2, range) ->
            SynExpr.NamedIndexedPropertySet
                (this.VisitLongIdentWithDots longDotId, this.VisitSynExpr e1, this.VisitSynExpr e2, range)
        | SynExpr.DotNamedIndexedPropertySet (expr, longDotId, e1, e2, range) ->
            SynExpr.DotNamedIndexedPropertySet
                (this.VisitSynExpr expr,
                 this.VisitLongIdentWithDots longDotId,
                 this.VisitSynExpr e1,
                 this.VisitSynExpr e2,
                 range)
        | SynExpr.TypeTest (expr, typeName, range) ->
            SynExpr.TypeTest(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Upcast (expr, typeName, range) ->
            SynExpr.Upcast(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.Downcast (expr, typeName, range) ->
            SynExpr.Downcast(this.VisitSynExpr expr, this.VisitSynType typeName, range)
        | SynExpr.InferredUpcast (expr, range) -> SynExpr.InferredUpcast(this.VisitSynExpr expr, range)
        | SynExpr.InferredDowncast (expr, range) -> SynExpr.InferredDowncast(this.VisitSynExpr expr, range)
        | SynExpr.Null (range) -> SynExpr.Null(range)
        | SynExpr.AddressOf (isByref, expr, refRange, range) ->
            SynExpr.AddressOf(isByref, this.VisitSynExpr expr, refRange, range)
        | SynExpr.TraitCall (typars, sign, expr, range) ->
            SynExpr.TraitCall
                (typars |> List.map this.VisitSynTypar, this.VisitSynMemberSig sign, this.VisitSynExpr expr, range)
        | SynExpr.JoinIn (expr, inrange, expr2, range) ->
            SynExpr.JoinIn(this.VisitSynExpr expr, inrange, this.VisitSynExpr expr2, range)
        | SynExpr.ImplicitZero (range) -> SynExpr.ImplicitZero(range)
        | SynExpr.YieldOrReturn (info, expr, range) -> SynExpr.YieldOrReturn(info, this.VisitSynExpr expr, range)
        | SynExpr.YieldOrReturnFrom (info, expr, range) ->
            SynExpr.YieldOrReturnFrom(info, this.VisitSynExpr expr, range)
        | SynExpr.LetOrUseBang (seqPoint, isUse, isFromSource, pat, rhsExpr, bodyExpr, range) ->
            SynExpr.LetOrUseBang
                (seqPoint,
                 isUse,
                 isFromSource,
                 this.VisitSynPat pat,
                 this.VisitSynExpr rhsExpr,
                 this.VisitSynExpr bodyExpr,
                 range)
        | SynExpr.MatchBang (seqPoint, expr, clauses, range) ->
            SynExpr.MatchBang(seqPoint, this.VisitSynExpr expr, clauses |> List.map this.VisitSynMatchClause, range)
        | SynExpr.DoBang (expr, range) -> SynExpr.DoBang(this.VisitSynExpr expr, range)
        | SynExpr.LibraryOnlyILAssembly (a, typs, exprs, typs2, range) ->
            SynExpr.LibraryOnlyILAssembly
                (a,
                 List.map this.VisitSynType typs,
                 List.map this.VisitSynExpr exprs,
                 List.map this.VisitSynType typs2,
                 range)
        | SynExpr.LibraryOnlyStaticOptimization (constraints, expr1, expr2, range) ->
            SynExpr.LibraryOnlyStaticOptimization(constraints, this.VisitSynExpr expr1, this.VisitSynExpr expr2, range)
        | SynExpr.LibraryOnlyUnionCaseFieldGet (expr, longId, i, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldGet(this.VisitSynExpr expr, this.VisitLongIdent longId, i, range)
        | SynExpr.LibraryOnlyUnionCaseFieldSet (e1, longId, i, e2, range) ->
            SynExpr.LibraryOnlyUnionCaseFieldSet
                (this.VisitSynExpr e1, this.VisitLongIdent longId, i, this.VisitSynExpr e2, range)
        | SynExpr.ArbitraryAfterError (debugStr, range) -> SynExpr.ArbitraryAfterError(debugStr, range)
        | SynExpr.FromParseError (expr, range) -> SynExpr.FromParseError(this.VisitSynExpr expr, range)
        | SynExpr.DiscardAfterMissingQualificationAfterDot (expr, range) ->
            SynExpr.DiscardAfterMissingQualificationAfterDot(this.VisitSynExpr expr, range)
        | SynExpr.Fixed (expr, range) -> SynExpr.Fixed(this.VisitSynExpr expr, range)

    abstract VisitRecordField: (RecordFieldName * SynExpr option * BlockSeparator option)
     -> RecordFieldName * SynExpr option * BlockSeparator option

    default this.VisitRecordField(((longId, correct), expr: SynExpr option, sep: BlockSeparator option)) =
        ((this.VisitLongIdentWithDots longId, correct), Option.map this.VisitSynExpr expr, sep)

    abstract VisitAnonRecordField: (Ident * SynExpr) -> Ident * SynExpr

    default this.VisitAnonRecordField((ident: Ident, expr: SynExpr)) =
        (this.VisitIdent ident, this.VisitSynExpr expr)

    abstract VisitAnonRecordTypeField: (Ident * SynType) -> Ident * SynType

    default this.VisitAnonRecordTypeField((ident: Ident, t: SynType)) =
        (this.VisitIdent ident, this.VisitSynType t)

    abstract VisitSynMemberSig: SynMemberSig -> SynMemberSig

    default this.VisitSynMemberSig(ms: SynMemberSig): SynMemberSig =
        match ms with
        | SynMemberSig.Member (valSig, flags, range) -> SynMemberSig.Member(this.VisitSynValSig valSig, flags, range)
        | SynMemberSig.Interface (typeName, range) -> SynMemberSig.Interface(this.VisitSynType typeName, range)
        | SynMemberSig.Inherit (typeName, range) -> SynMemberSig.Inherit(this.VisitSynType typeName, range)
        | SynMemberSig.ValField (f, range) -> SynMemberSig.ValField(this.VisitSynField f, range)
        | SynMemberSig.NestedType (typedef, range) -> SynMemberSig.NestedType(this.VisitSynTypeDefnSig typedef, range)

    abstract VisitSynIndexerArg: SynIndexerArg -> SynIndexerArg

    default this.VisitSynIndexerArg(ia: SynIndexerArg): SynIndexerArg =
        match ia with
        | SynIndexerArg.One (e) -> SynIndexerArg.One(this.VisitSynExpr e)
        | SynIndexerArg.Two (e1, e2) -> SynIndexerArg.Two(this.VisitSynExpr e1, this.VisitSynExpr e2)

    abstract VisitSynMatchClause: SynMatchClause -> SynMatchClause

    default this.VisitSynMatchClause(mc: SynMatchClause): SynMatchClause =
        match mc with
        | SynMatchClause.Clause (pat, e1, e2, range, pi) ->
            SynMatchClause.Clause(this.VisitSynPat pat, Option.map this.VisitSynExpr e1, e2, range, pi)

    abstract VisitArgsOption: (SynExpr * Ident option) -> SynExpr * Ident option

    default this.VisitArgsOption((expr: SynExpr, ident: Ident option)) =
        (this.VisitSynExpr expr, Option.map this.VisitIdent ident)

    abstract VisitSynInterfaceImpl: SynInterfaceImpl -> SynInterfaceImpl

    default this.VisitSynInterfaceImpl(ii: SynInterfaceImpl): SynInterfaceImpl =
        match ii with
        | InterfaceImpl (typ, bindings, range) ->
            InterfaceImpl(this.VisitSynType typ, bindings |> List.map this.VisitSynBinding, range)

    abstract VisitSynTypeDefn: SynTypeDefn -> SynTypeDefn

    default this.VisitSynTypeDefn(td: SynTypeDefn): SynTypeDefn =
        match td with
        | TypeDefn (sci, stdr, members, range) ->
            TypeDefn
                (this.VisitSynComponentInfo sci,
                 this.VisitSynTypeDefnRepr stdr,
                 members |> List.map this.VisitSynMemberDefn,
                 range)

    abstract VisitSynTypeDefnSig: SynTypeDefnSig -> SynTypeDefnSig

    default this.VisitSynTypeDefnSig(typeDefSig: SynTypeDefnSig): SynTypeDefnSig =
        match typeDefSig with
        | TypeDefnSig (sci, synTypeDefnSigReprs, memberSig, range) ->
            TypeDefnSig
                (this.VisitSynComponentInfo sci,
                 this.VisitSynTypeDefnSigRepr synTypeDefnSigReprs,
                 memberSig |> List.map this.VisitSynMemberSig,
                 range)

    abstract VisitSynTypeDefnSigRepr: SynTypeDefnSigRepr -> SynTypeDefnSigRepr

    default this.VisitSynTypeDefnSigRepr(stdr: SynTypeDefnSigRepr): SynTypeDefnSigRepr =
        match stdr with
        | SynTypeDefnSigRepr.ObjectModel (kind, members, range) ->
            SynTypeDefnSigRepr.ObjectModel
                (this.VisitSynTypeDefnKind kind, members |> List.map this.VisitSynMemberSig, range)
        | SynTypeDefnSigRepr.Simple (simpleRepr, range) ->
            SynTypeDefnSigRepr.Simple(this.VisitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnSigRepr.Exception (exceptionRepr) ->
            SynTypeDefnSigRepr.Exception(this.VisitSynExceptionDefnRepr exceptionRepr)

    abstract VisitSynMemberDefn: SynMemberDefn -> SynMemberDefn

    default this.VisitSynMemberDefn(mbrDef: SynMemberDefn): SynMemberDefn =
        match mbrDef with
        | SynMemberDefn.Open (longIdent, range) -> SynMemberDefn.Open(this.VisitLongIdent longIdent, range)
        | SynMemberDefn.Member (memberDefn, range) -> SynMemberDefn.Member(this.VisitSynBinding memberDefn, range)
        | SynMemberDefn.ImplicitCtor (access, attrs, ctorArgs, selfIdentifier, range) ->
            SynMemberDefn.ImplicitCtor
                (Option.map this.VisitSynAccess access,
                 attrs |> List.map this.VisitSynAttributeList,
                 this.VisitSynSimplePats ctorArgs,
                 Option.map this.VisitIdent selfIdentifier,
                 range)
        | SynMemberDefn.ImplicitInherit (inheritType, inheritArgs, inheritAlias, range) ->
            SynMemberDefn.ImplicitInherit
                (this.VisitSynType inheritType,
                 this.VisitSynExpr inheritArgs,
                 Option.map this.VisitIdent inheritAlias,
                 range)
        | SynMemberDefn.LetBindings (bindings, isStatic, isRecursive, range) ->
            SynMemberDefn.LetBindings(bindings |> List.map this.VisitSynBinding, isStatic, isRecursive, range)
        | SynMemberDefn.AbstractSlot (valSig, flags, range) ->
            SynMemberDefn.AbstractSlot(this.VisitSynValSig valSig, flags, range)
        | SynMemberDefn.Interface (typ, members, range) ->
            SynMemberDefn.Interface(this.VisitSynType typ, Option.map (List.map this.VisitSynMemberDefn) members, range)
        | SynMemberDefn.Inherit (typ, ident, range) ->
            SynMemberDefn.Inherit(this.VisitSynType typ, Option.map this.VisitIdent ident, range)
        | SynMemberDefn.ValField (fld, range) -> SynMemberDefn.ValField(this.VisitSynField fld, range)
        | SynMemberDefn.NestedType (typeDefn, access, range) ->
            SynMemberDefn.NestedType(this.VisitSynTypeDefn typeDefn, Option.map this.VisitSynAccess access, range)
        | SynMemberDefn.AutoProperty (attrs,
                                      isStatic,
                                      ident,
                                      typeOpt,
                                      propKind,
                                      flags,
                                      doc,
                                      access,
                                      synExpr,
                                      getSetRange,
                                      range) ->
            SynMemberDefn.AutoProperty
                (attrs |> List.map this.VisitSynAttributeList,
                 isStatic,
                 this.VisitIdent ident,
                 Option.map this.VisitSynType typeOpt,
                 propKind,
                 flags,
                 doc,
                 Option.map this.VisitSynAccess access,
                 this.VisitSynExpr synExpr,
                 getSetRange,
                 range)

    abstract VisitSynSimplePat: SynSimplePat -> SynSimplePat

    default this.VisitSynSimplePat(sp: SynSimplePat): SynSimplePat =
        match sp with
        | SynSimplePat.Id (ident, altName, isCompilerGenerated, isThisVar, isOptArg, range) ->
            SynSimplePat.Id(this.VisitIdent ident, altName, isCompilerGenerated, isThisVar, isOptArg, range)
        | SynSimplePat.Typed (simplePat, typ, range) ->
            SynSimplePat.Typed(this.VisitSynSimplePat simplePat, this.VisitSynType typ, range)
        | SynSimplePat.Attrib (simplePat, attrs, range) ->
            SynSimplePat.Attrib(this.VisitSynSimplePat simplePat, attrs |> List.map this.VisitSynAttributeList, range)

    abstract VisitSynSimplePats: SynSimplePats -> SynSimplePats

    default this.VisitSynSimplePats(sp: SynSimplePats): SynSimplePats =
        match sp with
        | SynSimplePats.SimplePats (pats, range) ->
            SynSimplePats.SimplePats(pats |> List.map this.VisitSynSimplePat, range)
        | SynSimplePats.Typed (pats, typ, range) ->
            SynSimplePats.Typed(this.VisitSynSimplePats pats, this.VisitSynType typ, range)

    abstract VisitSynBinding: SynBinding -> SynBinding

    default this.VisitSynBinding(binding: SynBinding): SynBinding =
        match binding with
        | Binding (access, kind, mustInline, isMutable, attrs, doc, valData, headPat, returnInfo, expr, range, seqPoint) ->
            Binding
                (Option.map this.VisitSynAccess access,
                 kind,
                 mustInline,
                 isMutable,
                 attrs |> List.map this.VisitSynAttributeList,
                 doc,
                 this.VisitSynValData valData,
                 this.VisitSynPat headPat,
                 Option.map this.VisitSynBindingReturnInfo returnInfo,
                 this.VisitSynExpr expr,
                 range,
                 seqPoint)

    abstract VisitSynValData: SynValData -> SynValData

    default this.VisitSynValData(svd: SynValData): SynValData =
        match svd with
        | SynValData (flags, svi, ident) ->
            SynValData(flags, this.VisitSynValInfo svi, Option.map this.VisitIdent ident)

    abstract VisitSynValSig: SynValSig -> SynValSig

    default this.VisitSynValSig(svs: SynValSig): SynValSig =
        match svs with
        | ValSpfn (attrs, ident, explicitValDecls, synType, arity, isInline, isMutable, doc, access, expr, range) ->
            ValSpfn
                (attrs |> List.map this.VisitSynAttributeList,
                 this.VisitIdent ident,
                 this.VisitSynValTyparDecls explicitValDecls,
                 this.VisitSynType synType,
                 this.VisitSynValInfo arity,
                 isInline,
                 isMutable,
                 doc,
                 Option.map this.VisitSynAccess access,
                 Option.map this.VisitSynExpr expr,
                 range)

    abstract VisitSynValTyparDecls: SynValTyparDecls -> SynValTyparDecls

    default this.VisitSynValTyparDecls(valTypeDecl: SynValTyparDecls): SynValTyparDecls =
        match valTypeDecl with
        | SynValTyparDecls (typardecls, b, constraints) ->
            SynValTyparDecls(typardecls |> List.map this.VisitSynTyparDecl, b, constraints)

    abstract VisitSynTyparDecl: SynTyparDecl -> SynTyparDecl

    default this.VisitSynTyparDecl(std: SynTyparDecl): SynTyparDecl =
        match std with
        | TyparDecl (attrs, typar) -> TyparDecl(attrs |> List.map this.VisitSynAttributeList, this.VisitSynTypar typar)

    abstract VisitSynTypar: SynTypar -> SynTypar

    default this.VisitSynTypar(typar: SynTypar): SynTypar =
        match typar with
        | Typar (ident, staticReq, isComGen) -> Typar(this.VisitIdent ident, staticReq, isComGen)

    abstract VisitTyparStaticReq: TyparStaticReq -> TyparStaticReq

    default this.VisitTyparStaticReq(tsr: TyparStaticReq): TyparStaticReq =
        match tsr with
        | NoStaticReq -> tsr
        | HeadTypeStaticReq -> tsr

    abstract VisitSynBindingReturnInfo: SynBindingReturnInfo -> SynBindingReturnInfo

    default this.VisitSynBindingReturnInfo(returnInfo: SynBindingReturnInfo): SynBindingReturnInfo =
        match returnInfo with
        | SynBindingReturnInfo (typeName, range, attrs) ->
            SynBindingReturnInfo(this.VisitSynType typeName, range, attrs |> List.map this.VisitSynAttributeList)

    abstract VisitSynPat: SynPat -> SynPat

    default this.VisitSynPat(sp: SynPat): SynPat =
        match sp with
        | SynPat.Const (sc, range) -> SynPat.Const(this.VisitSynConst sc, range)
        | SynPat.Wild (range) -> SynPat.Wild(range)
        | SynPat.Named (synPat, ident, isSelfIdentifier, access, range) ->
            SynPat.Named
                (this.VisitSynPat synPat,
                 this.VisitIdent ident,
                 isSelfIdentifier,
                 Option.map this.VisitSynAccess access,
                 range)
        | SynPat.Typed (synPat, synType, range) ->
            SynPat.Typed(this.VisitSynPat synPat, this.VisitSynType synType, range)
        | SynPat.Attrib (synPat, attrs, range) ->
            SynPat.Attrib(this.VisitSynPat synPat, attrs |> List.map this.VisitSynAttributeList, range)
        | SynPat.Or (synPat, synPat2, range) -> SynPat.Or(this.VisitSynPat synPat, this.VisitSynPat synPat2, range)
        | SynPat.Ands (pats, range) -> SynPat.Ands(pats |> List.map this.VisitSynPat, range)
        | SynPat.LongIdent (longDotId, ident, svtd, ctorArgs, access, range) ->
            SynPat.LongIdent
                (this.VisitLongIdentWithDots longDotId,
                 Option.map this.VisitIdent ident,
                 Option.map this.VisitSynValTyparDecls svtd,
                 this.VisitSynConstructorArgs ctorArgs,
                 Option.map this.VisitSynAccess access,
                 range)
        | SynPat.Tuple (isStruct, pats, range) -> SynPat.Tuple(isStruct, pats |> List.map this.VisitSynPat, range)
        | SynPat.Paren (pat, range) -> SynPat.Paren(this.VisitSynPat pat, range)
        | SynPat.ArrayOrList (isList, pats, range) ->
            SynPat.ArrayOrList(isList, pats |> List.map this.VisitSynPat, range)
        | SynPat.Record (pats, range) ->
            SynPat.Record
                (pats
                 |> List.map (fun ((longIdent, ident), pat) ->
                     ((this.VisitLongIdent longIdent, this.VisitIdent ident), this.VisitSynPat pat)),
                 range)
        | SynPat.Null (range) -> SynPat.Null(range)
        | SynPat.OptionalVal (ident, range) -> SynPat.OptionalVal(this.VisitIdent ident, range)
        | SynPat.IsInst (typ, range) -> SynPat.IsInst(this.VisitSynType typ, range)
        | SynPat.QuoteExpr (expr, range) -> SynPat.QuoteExpr(this.VisitSynExpr expr, range)
        | SynPat.DeprecatedCharRange (c, c2, range) -> SynPat.DeprecatedCharRange(c, c2, range)
        | SynPat.InstanceMember (ident, ident2, ident3, access, range) ->
            SynPat.InstanceMember
                (this.VisitIdent ident,
                 this.VisitIdent ident2,
                 Option.map this.VisitIdent ident3,
                 Option.map this.VisitSynAccess access,
                 range)
        | SynPat.FromParseError (pat, range) -> SynPat.FromParseError(this.VisitSynPat pat, range)

    abstract VisitSynConstructorArgs: SynConstructorArgs -> SynConstructorArgs

    default this.VisitSynConstructorArgs(ctorArgs: SynConstructorArgs): SynConstructorArgs =
        match ctorArgs with
        | Pats (pats) -> Pats(pats |> List.map this.VisitSynPat)
        | NamePatPairs (pats, range) ->
            NamePatPairs
                (pats
                 |> List.map (fun (ident, pat) -> (this.VisitIdent ident, this.VisitSynPat pat)),
                 range)

    abstract VisitSynComponentInfo: SynComponentInfo -> SynComponentInfo

    default this.VisitSynComponentInfo(sci: SynComponentInfo): SynComponentInfo =
        match sci with
        | ComponentInfo (attribs, typeParams, constraints, longId, doc, preferPostfix, access, range) ->
            ComponentInfo
                (attribs |> List.map this.VisitSynAttributeList,
                 typeParams |> List.map (this.VisitSynTyparDecl),
                 constraints,
                 longId,
                 doc,
                 preferPostfix,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynTypeDefnRepr: SynTypeDefnRepr -> SynTypeDefnRepr

    default this.VisitSynTypeDefnRepr(stdr: SynTypeDefnRepr): SynTypeDefnRepr =
        match stdr with
        | SynTypeDefnRepr.ObjectModel (kind, members, range) ->
            SynTypeDefnRepr.ObjectModel
                (this.VisitSynTypeDefnKind kind, members |> List.map this.VisitSynMemberDefn, range)
        | SynTypeDefnRepr.Simple (simpleRepr, range) ->
            SynTypeDefnRepr.Simple(this.VisitSynTypeDefnSimpleRepr simpleRepr, range)
        | SynTypeDefnRepr.Exception (exceptionRepr) ->
            SynTypeDefnRepr.Exception(this.VisitSynExceptionDefnRepr exceptionRepr)

    abstract VisitSynTypeDefnKind: SynTypeDefnKind -> SynTypeDefnKind

    default this.VisitSynTypeDefnKind(kind: SynTypeDefnKind): SynTypeDefnKind =
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
        | TyconDelegate (typ, valinfo) -> TyconDelegate(this.VisitSynType typ, this.VisitSynValInfo valinfo)

    abstract VisitSynTypeDefnSimpleRepr: SynTypeDefnSimpleRepr -> SynTypeDefnSimpleRepr

    default this.VisitSynTypeDefnSimpleRepr(arg: SynTypeDefnSimpleRepr): SynTypeDefnSimpleRepr =
        match arg with
        | SynTypeDefnSimpleRepr.None (range) -> SynTypeDefnSimpleRepr.None(range)
        | SynTypeDefnSimpleRepr.Union (access, unionCases, range) ->
            SynTypeDefnSimpleRepr.Union
                (Option.map this.VisitSynAccess access, unionCases |> List.map this.VisitSynUnionCase, range)
        | SynTypeDefnSimpleRepr.Enum (enumCases, range) ->
            SynTypeDefnSimpleRepr.Enum(enumCases |> List.map this.VisitSynEnumCase, range)
        | SynTypeDefnSimpleRepr.Record (access, recordFields, range) ->
            SynTypeDefnSimpleRepr.Record
                (Option.map this.VisitSynAccess access, recordFields |> List.map this.VisitSynField, range)
        | SynTypeDefnSimpleRepr.General (typeDefKind, a, b, c, d, e, pats, range) ->
            SynTypeDefnSimpleRepr.General(this.VisitSynTypeDefnKind typeDefKind, a, b, c, d, e, pats, range) // TODO
        | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly (ilType, range) ->
            SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(ilType, range)
        | SynTypeDefnSimpleRepr.TypeAbbrev (parserDetail, typ, range) ->
            SynTypeDefnSimpleRepr.TypeAbbrev(parserDetail, this.VisitSynType typ, range)
        | SynTypeDefnSimpleRepr.Exception (edr) -> SynTypeDefnSimpleRepr.Exception(this.VisitSynExceptionDefnRepr edr)

    abstract VisitSynExceptionDefn: SynExceptionDefn -> SynExceptionDefn

    default this.VisitSynExceptionDefn(exceptionDef: SynExceptionDefn): SynExceptionDefn =
        match exceptionDef with
        | SynExceptionDefn (sedr, members, range) ->
            SynExceptionDefn(this.VisitSynExceptionDefnRepr sedr, members |> List.map this.VisitSynMemberDefn, range)

    abstract VisitSynExceptionDefnRepr: SynExceptionDefnRepr -> SynExceptionDefnRepr

    default this.VisitSynExceptionDefnRepr(sedr: SynExceptionDefnRepr): SynExceptionDefnRepr =
        match sedr with
        | SynExceptionDefnRepr (attrs, unionCase, longId, doc, access, range) ->
            SynExceptionDefnRepr
                (attrs |> List.map this.VisitSynAttributeList,
                 this.VisitSynUnionCase unionCase,
                 longId,
                 doc,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynAttribute: SynAttribute -> SynAttribute

    default this.VisitSynAttribute(attr: SynAttribute): SynAttribute =
        { attr with
              ArgExpr = this.VisitSynExpr attr.ArgExpr
              Target = Option.map this.VisitIdent attr.Target }

    abstract VisitSynAttributeList: SynAttributeList -> SynAttributeList

    default this.VisitSynAttributeList(attrs: SynAttributeList): SynAttributeList =
        { attrs with
              Attributes =
                  attrs.Attributes
                  |> List.map this.VisitSynAttribute }

    abstract VisitSynUnionCase: SynUnionCase -> SynUnionCase

    default this.VisitSynUnionCase(uc: SynUnionCase): SynUnionCase =
        match uc with
        | UnionCase (attrs, ident, uct, doc, access, range) ->
            UnionCase
                (attrs |> List.map this.VisitSynAttributeList,
                 this.VisitIdent ident,
                 this.VisitSynUnionCaseType uct,
                 doc,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynUnionCaseType: SynUnionCaseType -> SynUnionCaseType

    default this.VisitSynUnionCaseType(uct: SynUnionCaseType): SynUnionCaseType =
        match uct with
        | UnionCaseFields (cases) -> UnionCaseFields(cases |> List.map this.VisitSynField)
        | UnionCaseFullType (stype, valInfo) -> UnionCaseFullType(this.VisitSynType stype, this.VisitSynValInfo valInfo)

    abstract VisitSynEnumCase: SynEnumCase -> SynEnumCase

    default this.VisitSynEnumCase(sec: SynEnumCase): SynEnumCase =
        match sec with
        | EnumCase (attrs, ident, cnst, doc, range) ->
            EnumCase
                (attrs |> List.map this.VisitSynAttributeList,
                 this.VisitIdent ident,
                 this.VisitSynConst cnst,
                 doc,
                 range)

    abstract VisitSynField: SynField -> SynField

    default this.VisitSynField(sfield: SynField): SynField =
        match sfield with
        | Field (attrs, isStatic, ident, typ, isMutable, doc, access, range) ->
            Field
                (attrs |> List.map this.VisitSynAttributeList,
                 isStatic,
                 Option.map this.VisitIdent ident,
                 this.VisitSynType typ,
                 isMutable,
                 doc,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynType: SynType -> SynType

    default this.VisitSynType(st: SynType): SynType =
        match st with
        | SynType.LongIdent (li) -> SynType.LongIdent(li)
        | SynType.App (typeName, lessRange, typeArgs, commaRanges, greaterRange, isPostfix, range) ->
            SynType.App
                (this.VisitSynType typeName,
                 lessRange,
                 typeArgs |> List.map this.VisitSynType,
                 commaRanges,
                 greaterRange,
                 isPostfix,
                 range)
        | SynType.LongIdentApp (typeName, longDotId, lessRange, typeArgs, commaRanges, greaterRange, range) ->
            SynType.LongIdentApp
                (this.VisitSynType typeName,
                 longDotId,
                 lessRange,
                 typeArgs |> List.map this.VisitSynType,
                 commaRanges,
                 greaterRange,
                 range)
        | SynType.Tuple (isStruct, typeNames, range) ->
            SynType.Tuple
                (isStruct,
                 typeNames
                 |> List.map (fun (b, typ) -> (b, this.VisitSynType typ)),
                 range)
        | SynType.Array (i, elementType, range) -> SynType.Array(i, this.VisitSynType elementType, range)
        | SynType.Fun (argType, returnType, range) ->
            SynType.Fun(this.VisitSynType argType, this.VisitSynType returnType, range)
        | SynType.Var (genericName, range) -> SynType.Var(this.VisitSynTypar genericName, range)
        | SynType.Anon (range) -> SynType.Anon(range)
        | SynType.WithGlobalConstraints (typeName, constraints, range) ->
            SynType.WithGlobalConstraints(this.VisitSynType typeName, constraints, range)
        | SynType.HashConstraint (synType, range) -> SynType.HashConstraint(this.VisitSynType synType, range)
        | SynType.MeasureDivide (dividendType, divisorType, range) ->
            SynType.MeasureDivide(this.VisitSynType dividendType, this.VisitSynType divisorType, range)
        | SynType.MeasurePower (measureType, cnst, range) ->
            SynType.MeasurePower(this.VisitSynType measureType, cnst, range)
        | SynType.StaticConstant (constant, range) -> SynType.StaticConstant(this.VisitSynConst constant, range)
        | SynType.StaticConstantExpr (expr, range) -> SynType.StaticConstantExpr(this.VisitSynExpr expr, range)
        | SynType.StaticConstantNamed (expr, typ, range) ->
            SynType.StaticConstantNamed(this.VisitSynType expr, this.VisitSynType typ, range)
        | SynType.AnonRecd (isStruct, typeNames, range) ->
            SynType.AnonRecd(isStruct, List.map this.VisitAnonRecordTypeField typeNames, range)

    abstract VisitSynConst: SynConst -> SynConst
    default this.VisitSynConst(sc: SynConst): SynConst = sc

    abstract VisitSynValInfo: SynValInfo -> SynValInfo

    default this.VisitSynValInfo(svi: SynValInfo): SynValInfo =
        match svi with
        | SynValInfo (args, arg) ->
            SynValInfo(args |> List.map (List.map this.VisitSynArgInfo), this.VisitSynArgInfo arg)

    abstract VisitSynArgInfo: SynArgInfo -> SynArgInfo

    default this.VisitSynArgInfo(sai: SynArgInfo): SynArgInfo =
        match sai with
        | SynArgInfo (attrs, optional, ident) ->
            SynArgInfo(attrs |> List.map this.VisitSynAttributeList, optional, Option.map this.VisitIdent ident)

    abstract VisitSynAccess: SynAccess -> SynAccess

    default this.VisitSynAccess(a: SynAccess): SynAccess =
        match a with
        | SynAccess.Private -> a
        | SynAccess.Internal -> a
        | SynAccess.Public -> a

    abstract VisitSynBindingKind: SynBindingKind -> SynBindingKind

    default this.VisitSynBindingKind(kind: SynBindingKind): SynBindingKind =
        match kind with
        | SynBindingKind.DoBinding -> kind
        | SynBindingKind.StandaloneExpression -> kind
        | SynBindingKind.NormalBinding -> kind

    abstract VisitMemberKind: MemberKind -> MemberKind

    default this.VisitMemberKind(mk: MemberKind): MemberKind =
        match mk with
        | MemberKind.ClassConstructor -> mk
        | MemberKind.Constructor -> mk
        | MemberKind.Member -> mk
        | MemberKind.PropertyGet -> mk
        | MemberKind.PropertySet -> mk
        | MemberKind.PropertyGetSet -> mk

    abstract VisitParsedHashDirective: ParsedHashDirective -> ParsedHashDirective

    default this.VisitParsedHashDirective(hash: ParsedHashDirective): ParsedHashDirective =
        match hash with
        | ParsedHashDirective (ident, longIdent, range) -> ParsedHashDirective(ident, longIdent, range)

    abstract VisitSynModuleOrNamespaceSig: SynModuleOrNamespaceSig -> SynModuleOrNamespaceSig

    default this.VisitSynModuleOrNamespaceSig(modOrNs: SynModuleOrNamespaceSig): SynModuleOrNamespaceSig =
        match modOrNs with
        | SynModuleOrNamespaceSig (longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            SynModuleOrNamespaceSig
                (this.VisitLongIdent longIdent,
                 isRecursive,
                 isModule,
                 decls |> List.map this.VisitSynModuleSigDecl,
                 doc,
                 attrs |> List.map this.VisitSynAttributeList,
                 Option.map this.VisitSynAccess access,
                 range)

    abstract VisitSynModuleSigDecl: SynModuleSigDecl -> SynModuleSigDecl

    default this.VisitSynModuleSigDecl(ast: SynModuleSigDecl): SynModuleSigDecl =
        match ast with
        | SynModuleSigDecl.ModuleAbbrev (ident, longIdent, range) ->
            SynModuleSigDecl.ModuleAbbrev(this.VisitIdent ident, this.VisitLongIdent longIdent, range)
        | SynModuleSigDecl.NestedModule (sci, isRecursive, decls, range) ->
            SynModuleSigDecl.NestedModule
                (this.VisitSynComponentInfo sci, isRecursive, decls |> List.map this.VisitSynModuleSigDecl, range)
        | SynModuleSigDecl.Val (node, range) -> SynModuleSigDecl.Val(this.VisitSynValSig node, range)
        | SynModuleSigDecl.Types (typeDefs, range) ->
            SynModuleSigDecl.Types(typeDefs |> List.map this.VisitSynTypeDefnSig, range)
        | SynModuleSigDecl.Open (longId, range) -> SynModuleSigDecl.Open(this.VisitLongIdent longId, range)
        | SynModuleSigDecl.HashDirective (hash, range) ->
            SynModuleSigDecl.HashDirective(this.VisitParsedHashDirective hash, range)
        | SynModuleSigDecl.NamespaceFragment (moduleOrNamespace) ->
            SynModuleSigDecl.NamespaceFragment(this.VisitSynModuleOrNamespaceSig moduleOrNamespace)
        | SynModuleSigDecl.Exception (synExceptionSig, range) ->
            SynModuleSigDecl.Exception(this.VisitSynExceptionSig synExceptionSig, range)

    abstract VisitSynExceptionSig: SynExceptionSig -> SynExceptionSig

    default this.VisitSynExceptionSig(exceptionDef: SynExceptionSig): SynExceptionSig =
        match exceptionDef with
        | SynExceptionSig (sedr, members, range) ->
            SynExceptionSig(this.VisitSynExceptionDefnRepr sedr, members |> List.map this.VisitSynMemberSig, range)

    abstract VisitLongIdentWithDots: LongIdentWithDots -> LongIdentWithDots

    default this.VisitLongIdentWithDots(lid: LongIdentWithDots): LongIdentWithDots =
        match lid with
        | LongIdentWithDots (ids, ranges) -> LongIdentWithDots(List.map this.VisitIdent ids, ranges)

    abstract VisitLongIdent: LongIdent -> LongIdent
    default this.VisitLongIdent(li: LongIdent): LongIdent = List.map this.VisitIdent li

    abstract VisitIdent: Ident -> Ident
    default this.VisitIdent(ident: Ident): Ident = ident
