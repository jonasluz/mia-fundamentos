﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace JALJ_MIA_Fundamentos
{
    class Analyzer
    {
        public List<string> Errors
        {
            get; private set;
        }

        public List<string> Log
        {
            get; private set;
        }

        public List<Token> Tokens
        {
            get
            {
                return m_tokenizer.Tokens;
            }
        }

        string m_expr;
        Tokenizer m_tokenizer; 

        public Analyzer(string expr)
        {
            m_expr = expr;
        }

        public bool Tokenize()
        {
            m_tokenizer = new Tokenizer();

            bool result = m_tokenizer.Tokenize(m_expr);
            if (!result) Errors = m_tokenizer.Error;

            return result;
        }

        public AST Parse()
        {
            if (m_tokenizer == null)
            {
                Log.Add("Tokenizing will be called automatically.");
                Tokenize();
            }

            List<Token> tokens = m_tokenizer.Tokens;

            Parser parser = new Parser();
            return parser.Parse(tokens);
        }
    }
}
