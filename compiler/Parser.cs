/**
  * Copyright (c) 2013 Stefan Marr,   stefan.marr@vub.ac.be
 * Copyright (c) 2009 Michael Haupt, michael.haupt@hpi.uni-potsdam.de
 * Software Architecture Group, Hasso Plattner Institute, Potsdam, Germany
 * http://www.hpi.uni-potsdam.de/swa/
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;
using System.Numerics;
using System.Text;
using static Som.Compiler.Symbol;

public class Parser
{
    protected string filename;
    protected Universe universe;
    protected ClassGenerationContext cgenc;
    protected Lexer lexer;


    protected Symbol sym;
    protected string text;
    protected Symbol nextSym;

    protected static BytecodeGenerator bcGen = new();
    protected static List<Symbol> singleOpSyms = new ();
    protected static List<Symbol> binaryOpSyms = new ();
    protected static List<Symbol> keywordSelectorSyms = new ();

    static Parser()
    {
        foreach (Symbol s in new Symbol[] {
            Not, And, Or, Star, Div, Mod, Plus, Equal, More, Less, Comma, At, Per, NONE})
        {
            singleOpSyms.Add(s);
        }
        foreach (Symbol s in new Symbol[] {Or, Comma, Minus, Equal, Not, And, Or, Star,
            Div, Mod, Plus, Equal, More, Less, Comma, At, Per, NONE})
        {
            binaryOpSyms.Add(s);
        }
        foreach (Symbol s in new Symbol[] { Keyword, KeywordSequence })
        {
            keywordSelectorSyms.Add(s);
        }
    }

    public class ParseError : ProgramDefinitionError
    {
        protected int line;
        protected int column;

        protected string text;
        protected string rawBuffer;
        protected string fileName;
        protected Symbol expected;
        protected Symbol found;

        public ParseError(string message, Symbol expected, Parser parser) : base(message)
        {
            if (parser.lexer == null)
            {
                this.line = 0;
                this.column = 0;
                this.rawBuffer = "";
            }
            else
            {
                this.line = parser.lexer.getCurrentLineNumber();
                this.column = parser.lexer.getCurrentColumn();
                this.rawBuffer = parser.lexer.getRawBuffer();
            }
            this.text = parser.text;
            this.fileName = parser.filename;
            this.expected = expected;
            this.found = parser.sym;
        }

        protected string expectedSymbolAsString() => expected.ToString();

        public override string Message
        {
            get
            {
                var msg = base.Message;

                var foundStr = Parser.printableSymbol(found) ? found + " (" + text + ")" : found.ToString();
                var expectedStr = expectedSymbolAsString();

                msg = msg.Replace("%(expected)s", expectedStr);
                msg = msg.Replace("%(found)s", foundStr);

                return msg;
            }
        }

        public override string ToString()
        {
            var msg = "%(file)s:%(line)d:%(column)d: error: " + base.Message;
            var foundStr = Parser.printableSymbol(found) ? found + " (" + text + ")" : found.ToString();
            msg += ": " + rawBuffer;
            var expectedStr = expectedSymbolAsString();

            msg = msg.Replace("%(file)s", fileName);
            msg = msg.Replace("%(line)d", "" + line);
            msg = msg.Replace("%(column)d", "" + column);
            msg = msg.Replace("%(expected)s", expectedStr);
            msg = msg.Replace("%(found)s", foundStr);
            return msg;
        }
    }

    public Parser(TextReader reader, Universe universe, string filename)
    {
        this.universe = universe;
        this.filename = filename;
        this.cgenc = new (universe);

        sym = NONE;
        lexer = new (reader);
        nextSym = NONE;
        getSymbolFromLexer();
    }

    public override string ToString() => filename + ":" + lexer.getCurrentLineNumber() + ":" + lexer.getCurrentColumn();

    public ClassGenerationContext classdef()
    {
        cgenc.Name = universe.symbolFor(text);
        this.expect(Identifier);
        this.expect(Equal);

        this.superclass();

        this.expect(NewTerm);
        this.classBody();

        if (this.accept(Separator))
        {
            this.cgenc.startClassSide();
            this.classBody();
        }
        this.expect(EndTerm);

        return this.cgenc;
    }

    private void classBody()
    {
        this.fields();
        while (this.symIsMethod())
        {
            var mgenc = new MethodGenerationContext(cgenc);
            mgenc.addArgument("self");

            this.method(mgenc);
            this.cgenc.addMethod(mgenc.assemble(universe));
        }
    }

    private bool symIsMethod() 
        => sym == Identifier 
        || sym == Keyword 
        || sym == OperatorSequence
        || symIn(binaryOpSyms)
        ;

    private void superclass()
    {
        SSymbol superName;
        if (sym == Identifier)
        {
            superName = universe.symbolFor(text);
            accept(Identifier);
        }
        else
        {
            superName = universe.symbolFor("Object");
        }
        cgenc.setSuperName(superName);

        // Load the super class, if it is not nil (break the dependency cycle)
        if (superName.getEmbeddedString()!=("nil"))
            initalizeFromSuperClass(superName);
    }

    private void initalizeFromSuperClass(SSymbol superName)
    {
        var superClass = universe.loadClass(superName);
        if (superClass == null)
            throw new ParseError(
                "Was not able to load super class: " + superName.getEmbeddedString(),
                Symbol.NONE, this);
        cgenc.setInstanceFieldsOfSuper(superClass.getInstanceFields());
        cgenc.setClassFieldsOfSuper(superClass.getSOMClass().getInstanceFields());
    }

    protected bool symIn(List<Symbol> ss) => ss.Contains(sym);

    protected bool accept(Symbol s)
    {
        if (sym == s)
        {
            getSymbolFromLexer();
            return true;
        }
        return false;
    }

    protected bool acceptOneOf(List<Symbol> ss)
    {
        if (symIn(ss))
        {
            getSymbolFromLexer();
            return true;
        }
        return false;
    }

    protected bool expect(Symbol s)
    {
        if (accept(s)) return true;
        var err = new StringBuilder("Error: " + filename + ":" +
            lexer.getCurrentLineNumber() +
            ": unexpected symbol, expected: " + s.ToString()
            + ", but found: " + sym.ToString());
        if (printableSymbol(sym))
        {
            err.Append(" (" + text + ")");
        }
        err.Append(": " + lexer.getRawBuffer());
        throw new IllegalStateException(err.ToString());
    }

    protected bool expectOneOf(List<Symbol> ss)
    {
        if (acceptOneOf(ss)) return true;
        var err = new StringBuilder("Error: " + filename + ":" +
            lexer.getCurrentLineNumber() + ": unexpected symbol, expected one of: ");
        foreach (Symbol s in ss)
        {
            err.Append(s.ToString() + ", ");
        }
        err.Append("but found: " + sym.ToString());
        if (printableSymbol(sym))
        {
            err.Append(" (" + text + ")");
        }
        err.Append(": " + lexer.getRawBuffer());
        throw new IllegalStateException(err.ToString());
    }

    protected void fields()
    {
        if (accept(Or))
        {
            while (sym == Identifier)
            {
                var var = variable();
                cgenc.addField(universe.symbolFor(var));
            }
            expect(Or);
        }
    }

    protected void method(MethodGenerationContext mgenc)
    {
        pattern(mgenc);
        expect(Equal);
        if (sym == Primitive)
        {
            mgenc.markAsPrimitive();
            primitiveBlock();
        }
        else
        {
            methodBlock(mgenc);
        }
    }

    protected void primitiveBlock() => expect(Primitive);

    private void pattern(MethodGenerationContext mgenc)
    {
        switch (sym)
        {
            case Identifier:
                unaryPattern(mgenc);
                break;
            case Keyword:
                keywordPattern(mgenc);
                break;
            default:
                binaryPattern(mgenc);
                break;
        }
    }

    protected void unaryPattern(MethodGenerationContext mgenc) => mgenc.setSignature(unarySelector());

    protected void binaryPattern(MethodGenerationContext mgenc)
    {
        mgenc.setSignature(binarySelector());
        mgenc.addArgumentIfAbsent(argument());
    }

    protected void keywordPattern(MethodGenerationContext mgenc)
    {
        var kw = new StringBuilder();
        do
        {
            kw.Append(keyword());
            mgenc.addArgumentIfAbsent(argument());
        } while (sym == Keyword);

        mgenc.setSignature(universe.symbolFor(kw.ToString()));
    }

    protected void methodBlock(MethodGenerationContext mgenc)
    {
        expect(NewTerm);
        blockContents(mgenc);
        // if no return has been generated so far, we can be sure there was no .
        // terminating the last expression, so the last expression's value must
        // be popped off the stack and a ^self be generated
        if (!mgenc.isFinished())
        {
            bcGen.emitPOP(mgenc);
            bcGen.emitPUSHARGUMENT(mgenc, (byte)0, (byte)0);
            bcGen.emitRETURNLOCAL(mgenc);
            mgenc.setFinished();
        }

        expect(EndTerm);
    }

    protected SSymbol unarySelector() => universe.symbolFor(identifier());

    protected SSymbol binarySelector()
    {
        var s = text;

        // Checkstyle: stop @formatter:off
        if (accept(Or))
        {
        }
        else if (accept(Comma))
        {
        }
        else if (accept(Minus))
        {
        }
        else if (accept(Equal))
        {
        }
        else if (acceptOneOf(singleOpSyms))
        {
        }
        else if (accept(OperatorSequence))
        {
        }
        else { expect(NONE); }
        // Checkstyle: resume @formatter:on

        return universe.symbolFor(s);
    }

    protected string identifier()
    {
        var s = text;
        bool isPrimitive = accept(Primitive);
        if (!isPrimitive) expect(Identifier);
        return s;
    }

    protected string keyword()
    {
        var s = text;
        expect(Keyword);
        return s;
    }

    protected string argument() => variable();

    protected void blockContents(MethodGenerationContext mgenc)
    {
        if (accept(Or))
        {
            locals(mgenc);
            expect(Or);
        }
        blockBody(mgenc, false);
    }

    protected void locals(MethodGenerationContext mgenc)
    {
        while (sym == Identifier)
        {
            mgenc.addLocalIfAbsent(variable());
        }
    }

    protected void blockBody(MethodGenerationContext mgenc, bool seenPeriod)
    {
        if (accept(Exit))
        {
            result(mgenc);
        }
        else if (sym == EndBlock)
        {
            if (seenPeriod)
            {
                // a POP has been generated which must be elided (blocks always
                // return the value of the last expression, regardless of
                // whether it
                // was terminated with a . or not)
                mgenc.removeLastBytecode();
            }
            if (mgenc.isBlockMethod() && !mgenc.hasBytecodes())
            {
                // if the block is empty, we need to return nil
                var nilSym = universe.symbolFor("nil");
                mgenc.addLiteralIfAbsent(nilSym, this);
                bcGen.emitPUSHGLOBAL(mgenc, nilSym);
            }
            bcGen.emitRETURNLOCAL(mgenc);
            mgenc.setFinished();
        }
        else if (sym == EndTerm)
        {
            // it does not matter whether a period has been seen, as the end of
            // the method has been found (EndTerm) - so it is safe to emit a "return
            // self"
            bcGen.emitPUSHARGUMENT(mgenc, (byte)0, (byte)0);
            bcGen.emitRETURNLOCAL(mgenc);
            mgenc.setFinished();
        }
        else
        {
            expression(mgenc);
            if (accept(Period))
            {
                bcGen.emitPOP(mgenc);
                blockBody(mgenc, true);
            }
        }
    }

    protected void result(MethodGenerationContext mgenc)
    {
        expression(mgenc);

        if (mgenc.isBlockMethod())
        {
            bcGen.emitRETURNNONLOCAL(mgenc);
        }
        else
        {
            bcGen.emitRETURNLOCAL(mgenc);
        }

        mgenc.markAsFinished();
        accept(Period);
    }

    protected void expression(MethodGenerationContext mgenc)
    {
        peekForNextSymbolFromLexer();

        if (nextSym == Assign)
        {
            assignation(mgenc);
        }
        else
        {
            evaluation(mgenc);
        }
    }

    protected void assignation(MethodGenerationContext mgenc)
    {
        List<string> list = new();

        assignments(mgenc, list);
        evaluation(mgenc);

        for (int i = 1; i <= list.Count; i++)
        {
            bcGen.emitDUP(mgenc);
        }
        foreach (string s in list)
        {
            genPopVariable(mgenc, s);
        }
    }

    protected void assignments(MethodGenerationContext mgenc, List<string> l)
    {
        if (sym == Identifier)
        {
            l.Add(assignment(mgenc));
            peekForNextSymbolFromLexer();
            if (nextSym == Assign)
            {
                assignments(mgenc, l);
            }
        }
    }

    protected string assignment(MethodGenerationContext mgenc)
    {
        var v = variable();
        expect(Assign);
        return v;
    }

    protected void evaluation(MethodGenerationContext mgenc)
    {
        var superSend = primary(mgenc);
        if (symIsMethod()) messages(mgenc, superSend);
    }

    protected bool primary(MethodGenerationContext mgenc)
    {
        bool superSend = false;
        switch (sym)
        {
            case Identifier:
                {
                    var v = variable();
                    if (v == ("super"))
                    {
                        superSend = true;
                        // sends to super push self as the receiver
                        v = "self";
                    }

                    genPushVariable(mgenc, v);
                    break;
                }
            case NewTerm:
                nestedTerm(mgenc);
                break;
            case NewBlock:
                {
                    var bgenc = new MethodGenerationContext(mgenc.getHolder(), mgenc);
                    nestedBlock(bgenc);

                    var blockMethod = bgenc.assembleMethod(universe);
                    mgenc.addLiteral(blockMethod, this);
                    bcGen.emitPUSHBLOCK(mgenc, blockMethod);
                    break;
                }
            default:
                literal(mgenc);
                break;
        }
        return superSend;
    }

    protected string variable() => identifier();

    protected void messages(MethodGenerationContext mgenc, bool superSend)
    {
        if (sym == Identifier)
        {
            do
            {
                // only the first message in a sequence can be a super send
                unaryMessage(mgenc, superSend);
                superSend = false;
            } while (sym == Identifier);

            while (sym == OperatorSequence || symIn(binaryOpSyms))
            {
                binaryMessage(mgenc, false);
            }

            if (sym == Keyword)
            {
                keywordMessage(mgenc, false);
            }
        }
        else if (sym == OperatorSequence || symIn(binaryOpSyms))
        {
            do
            {
                // only the first message in a sequence can be a super send
                binaryMessage(mgenc, superSend);
                superSend = false;
            } while (sym == OperatorSequence || symIn(binaryOpSyms));

            if (sym == Keyword)
            {
                keywordMessage(mgenc, false);
            }
        }
        else
        {
            keywordMessage(mgenc, superSend);
        }
    }

    protected void unaryMessage(MethodGenerationContext mgenc,bool superSend)
    {
        var msg = unarySelector();
        mgenc.addLiteralIfAbsent(msg, this);

        if (superSend)
        {
            bcGen.emitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.emitSEND(mgenc, msg);
        }
    }

    protected void binaryMessage(MethodGenerationContext mgenc, bool superSend)
    {
        var msg = binarySelector();
        mgenc.addLiteralIfAbsent(msg, this);

        binaryOperand(mgenc);

        if (superSend)
        {
            bcGen.emitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.emitSEND(mgenc, msg);
        }
    }

    protected bool binaryOperand(MethodGenerationContext mgenc)
    {
        bool superSend = this.primary(mgenc);

        while (sym == Identifier)
        {
            this.unaryMessage(mgenc, superSend);
            superSend = false;
        }

        return superSend;
    }

    protected void keywordMessage(MethodGenerationContext mgenc, bool superSend)
    {
        var kw = new StringBuilder();
        do
        {
            kw.Append(keyword());
            formula(mgenc);
        } while (sym == Keyword);

        var msg = universe.symbolFor(kw.ToString());

        mgenc.addLiteralIfAbsent(msg, this);

        if (superSend)
        {
            bcGen.emitSUPERSEND(mgenc, msg);
        }
        else
        {
            bcGen.emitSEND(mgenc, msg);
        }
    }

    protected void formula(MethodGenerationContext mgenc)
    {
        var superSend = binaryOperand(mgenc);

        // only the first message in a sequence can be a super send
        if (sym == OperatorSequence || symIn(binaryOpSyms))
        {
            binaryMessage(mgenc, superSend);
        }

        while (sym == OperatorSequence || symIn(binaryOpSyms))
        {
            binaryMessage(mgenc, false);
        }
    }

    protected void nestedTerm(MethodGenerationContext mgenc)
    {
        this.expect(NewTerm);
        this.expression(mgenc);
        this.expect(EndTerm);
    }

    protected void literal(MethodGenerationContext mgenc)
    {
        switch (sym)
        {
            case Pound:
                {
                    this.peekForNextSymbolFromLexerIfNecessary();
                    if (nextSym == NewTerm)
                    {
                        this.literalArray(mgenc);
                    }
                    else
                    {
                        this.literalSymbol(mgenc);
                    }
                    break;
                }
            case STString:
                {
                    literalString(mgenc);
                    break;
                }
            default:
                {
                    literalNumber(mgenc);
                    break;
                }
        }
    }

    protected void literalNumber(MethodGenerationContext mgenc)
    {
        var lit = sym == Minus ? negativeDecimal() : literalDecimal(false);
        mgenc.addLiteralIfAbsent(lit, this);
        bcGen.emitPUSHCONSTANT(mgenc, lit);
    }

    protected SAbstractObject literalDecimal(bool isNegative) => sym == Integer ? literalInteger(isNegative) : literalDouble(isNegative);

    protected SAbstractObject negativeDecimal()
    {
        this.expect(Minus);
        return this.literalDecimal(true);
    }

    protected SAbstractObject literalInteger(bool isNegative)
    {
        if (long.TryParse(text, out var i))
        {
            if (isNegative) i = -i;
            this.expect(Integer);
            return this.universe.newInteger(i);
        }
        else
        {
            if (BigInteger.TryParse(text, out var big))
            {
                if (isNegative) big = -big;
                this.expect(Integer);
                return this.universe.newBigInteger(big);
            }
            else
            {
                var err = "Error: " + filename + ":" +
                    lexer.getCurrentLineNumber() +
                    ": parsing number literal failed: '" + text.ToString()
                    + "'";
                throw new IllegalStateException(err);
            }
        }
    }

    protected SAbstractObject literalDouble(bool isNegative)
    {
        if (double.TryParse(text, out var d))
        {
            if (isNegative) d = -d;
            this.expect(Double);
            return this.universe.newDouble(d);
        }
        else
        {
            throw new ParseError("Could not parse double. Expected a number but " +
                "got '" + text + "'", NONE, this);
        }
    }

    protected void literalSymbol(MethodGenerationContext mgenc)
    {
        expect(Pound);
        var symb = sym == STString ? universe.symbolFor(_string()) : selector();
        mgenc.addLiteralIfAbsent(symb, this);
        bcGen.emitPUSHCONSTANT(mgenc, symb);
    }

    protected void literalString(MethodGenerationContext mgenc)
    {
        var s = this._string();
        var str = this.universe.newString(s);
        mgenc.addLiteralIfAbsent(str, this);
        bcGen.emitPUSHCONSTANT(mgenc, str);
    }

    protected void literalArray(MethodGenerationContext mgenc)
    {
        this.expect(Pound);
        this.expect(NewTerm);
        var arrayClassName = universe.symbolFor("Array");
        var arraySizePlaceholder = universe.symbolFor("ArraySizeLiteralPlaceholder");
        var newMessage = universe.symbolFor("new:");
        var atPutMessage = universe.symbolFor("at:put:");

        mgenc.addLiteralIfAbsent(arrayClassName, this);
        mgenc.addLiteralIfAbsent(newMessage, this);
        mgenc.addLiteralIfAbsent(atPutMessage, this);
        byte arraySizeLiteralIndex = mgenc.addLiteral(arraySizePlaceholder, this);

        // create empty array
        bcGen.emitPUSHGLOBAL(mgenc, arrayClassName);
        bcGen.emitPUSHCONSTANT(mgenc, arraySizeLiteralIndex);
        bcGen.emitSEND(mgenc, newMessage);

        int i = 1;

        while (sym != EndTerm)
        {
            var pushIndex = universe.newInteger(i);
            mgenc.addLiteralIfAbsent(pushIndex, this);
            bcGen.emitPUSHCONSTANT(mgenc, pushIndex);
            literal(mgenc);
            bcGen.emitSEND(mgenc, atPutMessage);
            i += 1;
        }

        // replace the placeholder with the actual array size
        mgenc.updateLiteral(
            arraySizePlaceholder, arraySizeLiteralIndex, universe.newInteger(i - 1));
        expect(EndTerm);
    }

    protected SSymbol selector() => 
            sym == OperatorSequence || symIn(singleOpSyms)
            ? binarySelector()
            :  sym == Keyword 
            || sym == KeywordSequence 
            ? keywordSelector() 
            : unarySelector()
        ;

    protected SSymbol keywordSelector()
    {
        var s = new string(text);
        expectOneOf(keywordSelectorSyms);
        return universe.symbolFor(s);
    }

    protected string _string() 
    {
        var s = new string(this.text);
        expect(STString);
        return s;
    }

    protected void nestedBlock(MethodGenerationContext mgenc)
    {
        mgenc.addArgumentIfAbsent("$block self");

        expect(NewBlock);
        if (sym == Colon) this.blockPattern(mgenc);

        // generate Block signature
        var blockSig = "$block method";
        int argSize = mgenc.getNumberOfArguments();
        for (int i = 1; i < argSize; i++) blockSig += ":";

        mgenc.setSignature(universe.symbolFor(blockSig));

        this.blockContents(mgenc);

        // if no return has been generated, we can be sure that the last
        // expression
        // in the block was not terminated by ., and can generate a return
        if (!mgenc.isFinished())
        {
            if (!mgenc.hasBytecodes())
            {
                // if the block is empty, we need to return nil
                var nilSym = universe.symbolFor("nil");
                mgenc.addLiteralIfAbsent(nilSym, this);
                bcGen.emitPUSHGLOBAL(mgenc, nilSym);
            }
            bcGen.emitRETURNLOCAL(mgenc);
            mgenc.markAsFinished();
        }

        this.expect(EndBlock);
    }

    protected void blockPattern(MethodGenerationContext mgenc)
    {
        this.blockArguments(mgenc);
        this.expect(Or);
    }

    protected void blockArguments(MethodGenerationContext mgenc)
    {
        do
        {
            expect(Colon);
            mgenc.addArgumentIfAbsent(argument());
        } while (sym == Colon);
    }

    protected void genPushVariable(MethodGenerationContext mgenc, string var)
    {
        // The purpose of this function is to find out whether the variable to be
        // pushed on the stack is a local variable, argument, or object field.
        // This is done by examining all available lexical contexts, starting with
        // the innermost (i.e., the one represented by mgenc).

        // triplet: index, context, isArgument
        Triplet<byte, byte, bool> tri = new (0, 0, false);

        if (mgenc.findVar(var, tri))
        {
            if (tri.Z)
            {
                bcGen.emitPUSHARGUMENT(mgenc, tri.X, tri.Y);
            }
            else
            {
                bcGen.emitPUSHLOCAL(mgenc, tri.X, tri.Y);
            }
        }
        else
        {
            var identifier = universe.symbolFor(var);
            if (mgenc.hasField(identifier))
            {
                var fieldName = identifier;
                mgenc.addLiteralIfAbsent(fieldName, this);
                bcGen.emitPUSHFIELD(mgenc, fieldName);
            }
            else
            {
                var global = identifier;
                mgenc.addLiteralIfAbsent(global, this);
                bcGen.emitPUSHGLOBAL(mgenc, global);
            }
        }
    }

    protected void genPopVariable(MethodGenerationContext mgenc, string var)
    {
        // The purpose of this function is to find out whether the variable to be
        // popped off the stack is a local variable, argument, or object field.
        // This is done by examining all available lexical contexts, starting with
        // the innermost (i.e., the one represented by mgenc).

        // triplet: index, context, isArgument
        Triplet<byte, byte, bool> tri = new (0, 0, false);

        if (mgenc.findVar(var, tri))
        {
            if (tri.Z)
            {
                bcGen.emitPOPARGUMENT(mgenc, tri.X, tri.Y);
            }
            else
            {
                bcGen.emitPOPLOCAL(mgenc, tri.X, tri.Y);
            }
        }
        else
        {
            var varName = universe.symbolFor(var);
            if (!mgenc.hasField(varName))
            {
                throw new ParseError("Trying to write to field with the name '" + var
                    + "', but field does not seem exist in class.", Symbol.NONE, this);
            }
            bcGen.emitPOPFIELD(mgenc, varName);
        }
    }

    protected void getSymbolFromLexer()
    {
        sym = lexer.getSym();
        text = lexer.getText();
    }

    protected void peekForNextSymbolFromLexerIfNecessary()
    {
        if (!lexer.getPeekDone()) peekForNextSymbolFromLexer();
    }

    protected void peekForNextSymbolFromLexer() => nextSym = lexer.peek();

    protected static bool printableSymbol(Symbol sym) 
        => sym == Integer || sym == Double || sym.CompareTo(STString) >= 0;
}
