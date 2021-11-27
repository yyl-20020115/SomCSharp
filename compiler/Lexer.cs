/**
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
using System.Text;

public class Lexer
{
    public static string SEPARATOR = "----";
    public static string PRIMITIVE = "primitive";

    protected int lineNumber;
    protected int charsRead; // all characters read, excluding the current line
    protected TextReader reader;
    protected Symbol sym;
    protected char symc;
    protected StringBuilder builder;
    protected bool peekDone;
    protected Symbol nextSym;
    protected char nextSymc;
    protected StringBuilder nextText;
    protected string buffer;
    protected int bufferPosition;

    public Lexer(TextReader reader)
    {
        this.reader = reader;
        this.peekDone = false;
        this.buffer = "";
        this.builder = new ();
        this.bufferPosition = 0;
        this.lineNumber = 0;
    }

    public bool getPeekDone() => peekDone;

    public Symbol getSym()
    {
        if (this.peekDone)
        {
            this.peekDone = false;
            this.sym = nextSym;
            this.symc = nextSymc;
            this.builder = new (nextText.ToString());
            return this.sym;
        }

        do
        {
            if (!this.hasMoreInput())
            {
                this.sym = Symbol.NONE;
                this.symc = '\0';
                this.builder = new (this.symc);
                return sym;
            }
            skipWhiteSpace();
            skipComment();
        } while (endOfBuffer() || char.IsWhiteSpace(currentChar()) || currentChar() == '"');

        if (this.currentChar() == '\'')
        {
            this.lexString();
        }
        else if (this.currentChar() == '[')
        {
            this.match(Symbol.NewBlock);
        }
        else if (this.currentChar() == ']')
        {
            this.match(Symbol.EndBlock);
        }
        else if (this.currentChar() == ':')
        {
            if (this.bufchar(bufferPosition + 1) == '=')
            {
                this.bufferPosition += 2;
                this.sym = Symbol.Assign;
                this.symc = '\0';
                this.builder = new (":=");
            }
            else
            {
                this.bufferPosition++;
                this.sym = Symbol.Colon;
                this.symc = ':';
                this.builder = new (":");
            }
        }
        else if (this.currentChar() == '(')
        {
            this.match(Symbol.NewTerm);
        }
        else if (this.currentChar() == ')')
        {
            this.match(Symbol.EndTerm);
        }
        else if (this.currentChar() == '#')
        {
            this.match(Symbol.Pound);
        }
        else if (this.currentChar() == '^')
        {
            this.match(Symbol.Exit);
        }
        else if (this.currentChar() == '.')
        {
            this.match(Symbol.Period);
        }
        else if (this.currentChar() == '-')
        {
            if (this.buffer.Substring(bufferPosition).StartsWith(SEPARATOR))
            {
                this.builder = new ();
                while(this.currentChar() == '-')
                {
                    this.builder.Append(bufchar(bufferPosition++));
                }
                this.sym = Symbol.Separator;
            }
            else
            {
                this.lexOperator();
            }
        }
        else if (isOperator(this.currentChar()))
        {
            this.lexOperator();
        }
        else if (this.buffer.Substring(bufferPosition).StartsWith(PRIMITIVE))
        {
            this.bufferPosition += PRIMITIVE.Length;
            this.sym = Symbol.Primitive;
            this.symc = '\0';
            this.builder = new (PRIMITIVE);
        }
        else if (char.IsLetter(currentChar()))
        {
            this.symc = '\0';
            this.builder = new ();
            while (char.IsLetterOrDigit(this.currentChar()) || this.currentChar() == '_')
            {
                this.builder.Append(bufchar(this.bufferPosition++));
            }
            this.sym = Symbol.Identifier;
            if (bufchar(this.bufferPosition) == ':')
            {
                this.sym = Symbol.Keyword;
                this.bufferPosition++;
                this.builder.Append(':');
                if (char.IsLetter(currentChar()))
                {
                    this.sym = Symbol.KeywordSequence;
                    while (char.IsLetter(this.currentChar()) || this.currentChar() == ':')
                    {
                        this.builder.Append(bufchar(bufferPosition++));
                    }
                }
            }
        }
        else if (char.IsDigit(this.currentChar()))
        {
            this.lexNumber();
        }
        else
        {
            this.sym = Symbol.NONE;
            this.symc = currentChar();
            this.builder = new (symc);
        }

        return this.sym;
    }

    private void lexNumber()
    {
        this.sym = Symbol.Integer;
        this.symc = '\0';
        this.builder = new ();

        bool sawDecimalMark = false;

        do
        {
            this.builder.Append(bufchar(this.bufferPosition++));
            if (!sawDecimalMark &&
                '.' == this.currentChar() &&
                char.IsDigit(bufchar(this.bufferPosition + 1)))
            {
                this.sym = Symbol.Double;
                this.builder.Append(bufchar(this.bufferPosition++));
            }
        } while (char.IsDigit(this.currentChar()));
    }

    private void lexEscapeChar()
    {
        //assert!endOfBuffer();
        var current = this.currentChar();
        switch (current)
        {
            // @formatter:off
            case 't': this.builder.Append("\t"); break;
            case 'b': this.builder.Append("\b"); break;
            case 'n': this.builder.Append("\n"); break;
            case 'r': this.builder.Append("\r"); break;
            case 'f': this.builder.Append("\f"); break;
            case '\'': this.builder.Append('\''); break;
            case '\\': this.builder.Append("\\"); break;
            case '0': this.builder.Append("\0"); break;
                // @formatter:on
        }
        this.bufferPosition++;
    }

    private void lexStringChar()
    {
        if (this.currentChar() == '\\')
        {
            this.bufferPosition++;
            this.lexEscapeChar();
        }
        else
        {
            this.builder.Append(this.currentChar());
            this.bufferPosition++;
        }
    }

    private void lexString()
    {
        this.sym = Symbol.STString;
        this.symc = '\0';
        this.builder = new ();
        this.bufferPosition++;

        while (this.currentChar() != '\'')
        {
            while (this.endOfBuffer())
            {
                if (this.fillBuffer() == -1) return;
                this.builder.Append('\n');
            }
            if (this.currentChar() != '\'')
            {
                this.lexStringChar();
            }
        }

        this.bufferPosition++;
    }

    private void lexOperator()
    {
        if (isOperator(bufchar(this.bufferPosition + 1)))
        {
            this.sym = Symbol.OperatorSequence;
            this.symc = '\0';
            this.builder = new ();
            while (isOperator(this.currentChar()))
            {
                this.builder.Append(bufchar(this.bufferPosition++));
            }
        }
        else if (this.currentChar() == '~')
        {
            match(Symbol.Not);
        }
        else if (this.currentChar() == '&')
        {
            match(Symbol.And);
        }
        else if (this.currentChar() == '|')
        {
            match(Symbol.Or);
        }
        else if (this.currentChar() == '*')
        {
            match(Symbol.Star);
        }
        else if (this.currentChar() == '/')
        {
            match(Symbol.Div);
        }
        else if (this.currentChar() == '\\')
        {
            match(Symbol.Mod);
        }
        else if (this.currentChar() == '+')
        {
            match(Symbol.Plus);
        }
        else if (this.currentChar() == '=')
        {
            match(Symbol.Equal);
        }
        else if (this.currentChar() == '>')
        {
            match(Symbol.More);
        }
        else if (this.currentChar() == '<')
        {
            match(Symbol.Less);
        }
        else if (this.currentChar() == ',')
        {
            match(Symbol.Comma);
        }
        else if (this.currentChar() == '@')
        {
            match(Symbol.At);
        }
        else if (this.currentChar() == '%')
        {
            match(Symbol.Per);
        }
        else if (this.currentChar() == '-')
        {
            match(Symbol.Minus);
        }
    }
    public Symbol peek()
    {
        var saveSym = sym;
        var saveSymc = symc;
        var saveText = new StringBuilder(this.builder.ToString());
        if (peekDone) throw new IllegalStateException("SOM lexer: cannot peek twice!");

        getSym();
        nextSym = sym;
        nextSymc = symc;
        nextText = new (builder.ToString());
        sym = saveSym;
        symc = saveSymc;
        builder = saveText;
        peekDone = true;
        return nextSym;
    }
    public string getText() => this.builder.ToString();

    public string getNextText() => this.nextText.ToString();

    public string getRawBuffer() => this.buffer;

    public int getCurrentLineNumber() => this.lineNumber;

    public int getCurrentColumn() => this.bufferPosition + 1;

    // All characters read and processed, including current line
    protected int getNumberOfCharactersRead() => this.charsRead + this.bufferPosition;

    private int fillBuffer()
    {
        try
        {
            if (reader.Peek() == -1) return -1;
            charsRead += buffer.Length;
            buffer = reader.ReadLine();
            if (buffer == null) return -1;
            ++lineNumber;
            bufferPosition = 0;
            return buffer.Length;
        }
        catch (IOException ioe)
        {
            throw new IllegalStateException("Error reading from input: " + ioe.ToString());
        }
    }

    private bool hasMoreInput()
    {
        while (endOfBuffer()) if (fillBuffer() == -1) return false;
        return true;
    }

    private void skipWhiteSpace()
    {
        while (char.IsWhiteSpace(currentChar()))
        {
            bufferPosition++;
            while (endOfBuffer()) if (fillBuffer() == -1) return;
        }
    }

    private void skipComment()
    {
        if (this.currentChar() == '"')
        {
            do
            {
                this.bufferPosition++;
                while (this.endOfBuffer())
                    if (this.fillBuffer() == -1) return;
            } while (this.currentChar() != '"');
            bufferPosition++;
        }
    }

    private char currentChar() => bufchar(this.bufferPosition);

    private bool endOfBuffer() => this.bufferPosition >= this.buffer.Length;

    public static bool isOperator(char c) =>
        c == '~' || c == '&' || c == '|' || c == '*' || c == '/'
            || c == '\\' || c == '+' || c == '=' || c == '>' || c == '<'
            || c == ',' || c == '@' || c == '%' || c == '-';

    private void match(Symbol s)
    {
        this.sym = s;
        this.symc = currentChar();
        this.builder = new (symc.ToString());
        this.bufferPosition++;
    }

    private char bufchar(int p) => p >= buffer.Length ? '\0' : buffer[p];
}