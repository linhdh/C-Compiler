﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class TestingClass {
    public TestingClass() {
        Console.WriteLine("constructor");
    }
}

namespace driver {
    class Program {
        static void Main(string[] args) {
            Scanner scanner = new Scanner();
            scanner.src =
                @"
int foo() {
	return 0;
}
int main(int argc, char **argv) {
    int b = 3 * 4;
    char c;
    float local_variable_2;
    double local_variable_3;
    const int * const * volatile a[3][4];
    b;
    foo();
    3.25f;
    ""3.0"";
    c;
}
                ";
            scanner.Lex();
            Console.WriteLine("Source code:");
            Console.WriteLine("======================");
            Console.WriteLine(scanner.src);

            Console.WriteLine("Tokens:");
            Console.WriteLine("======================");
            Console.WriteLine(scanner);

            List<Token> tokens = scanner.tokens;

            SyntaxTree.TranslationUnit unit;
			if (_translation_unit.Parse(tokens, 0, out unit) != tokens.Count - 1) {
				throw new InvalidOperationException("Error: not finished parsing");
			}

            Tuple<AST.Env, AST.TranslnUnit> ast = unit.GetTranslationUnit();

            CGenState state = new CGenState();
            ast.Item2.CodeGenerate(state);

            Console.WriteLine("x86 Assembly:");
            Console.WriteLine("======================");
            Console.WriteLine(state);


        }
    }
}