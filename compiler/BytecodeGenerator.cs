/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
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
using Som.VMObject;
using static Som.Interpreter.Bytecodes;

public class BytecodeGenerator
{
    public void emitPOP(MethodGenerationContext mgenc) => emit1(mgenc, POP);
    public void emitPUSHARGUMENT(MethodGenerationContext mgenc, byte idx, byte ctx) => emit3(mgenc, PUSH_ARGUMENT, idx, ctx);
    public void emitRETURNLOCAL(MethodGenerationContext mgenc) => emit1(mgenc, RETURN_LOCAL);
    public void emitRETURNNONLOCAL(MethodGenerationContext mgenc) => emit1(mgenc, RETURN_NON_LOCAL);
    public void emitDUP(MethodGenerationContext mgenc) => emit1(mgenc, DUP);
    public void emitPUSHBLOCK(MethodGenerationContext mgenc, SMethod blockMethod) => emit2(mgenc, PUSH_BLOCK, mgenc.findLiteralIndex(blockMethod));
    public void emitPUSHLOCAL(MethodGenerationContext mgenc, byte idx, byte ctx) => emit3(mgenc, PUSH_LOCAL, idx, ctx);
    public void emitPUSHFIELD(MethodGenerationContext mgenc, SSymbol fieldName) => emit2(mgenc, PUSH_FIELD, mgenc.getFieldIndex(fieldName));
    public void emitPUSHGLOBAL(MethodGenerationContext mgenc, SSymbol global) => emit2(mgenc, PUSH_GLOBAL, mgenc.findLiteralIndex(global));
    public void emitPOPARGUMENT(MethodGenerationContext mgenc, byte idx, byte ctx) => emit3(mgenc, POP_ARGUMENT, idx, ctx);
    public void emitPOPLOCAL(MethodGenerationContext mgenc, byte idx, byte ctx) => emit3(mgenc, POP_LOCAL, idx, ctx);
    public void emitPOPFIELD(MethodGenerationContext mgenc, SSymbol fieldName) => emit2(mgenc, POP_FIELD, mgenc.getFieldIndex(fieldName));
    public void emitSUPERSEND(MethodGenerationContext mgenc, SSymbol msg) => emit2(mgenc, SUPER_SEND, mgenc.findLiteralIndex(msg));
    public void emitSEND(MethodGenerationContext mgenc, SSymbol msg) => emit2(mgenc, SEND, mgenc.findLiteralIndex(msg));
    public void emitPUSHCONSTANT(MethodGenerationContext mgenc, SAbstractObject lit) => emit2(mgenc, PUSH_CONSTANT, mgenc.findLiteralIndex(lit));
    public void emitPUSHCONSTANT(MethodGenerationContext mgenc, byte literalIndex) => emit2(mgenc, PUSH_CONSTANT, literalIndex);
    private void emit1(MethodGenerationContext mgenc, byte code) => mgenc.addBytecode(code);
    private void emit2(MethodGenerationContext mgenc, byte code, byte idx) => mgenc.addBytecode(code).addBytecode(idx);
    private void emit3(MethodGenerationContext mgenc, byte code, byte idx, byte ctx) => mgenc.addBytecode(code).addBytecode(idx).addBytecode(ctx);
}
