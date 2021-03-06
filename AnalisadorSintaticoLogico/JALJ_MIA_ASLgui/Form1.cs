﻿using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using JALJ_MIA_ASLlib;

namespace JALJ_MIA_ASLgui
{
    /// <summary>
    /// The main application form.
    /// </summary>
    public partial class FormMain : Form
    {
        TreeFiller m_treeFiller = null;
        List<MultipleDisjunction> m_premisses = new List<MultipleDisjunction>();
        List<MultipleDisjunction> m_theorem = new List<MultipleDisjunction>();
        ATP atp = new ATP(); 

        #region Created by Form Designer

        public FormMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) { ; }

        #endregion Created by Form Designer

        #region Buttons actions

        // Create the Analytic Sintatic Tree.
        private void buttonAst_Click(object sender, EventArgs e)
        {
            string expr = textBoxInput.Text;    // expression input.

            // Cleanup and setup.
            ClearErrorMsgs();
            tabControl1.SelectedIndex = 0;

            // Creates a new analyzer for the expression.
            Analyzer asl = new Analyzer(expr);
            // Tokenization phase.
            if (!Tokenize(asl)) return;
            // Parsing phase.
            AST ast = asl.Parse();

            // Add the tree to the image.
            if (m_treeFiller == null) m_treeFiller = new TreeFiller(pictureBoxTree);
            m_treeFiller.Draw(ast);
        }

        // Convert the formula to Conjunctive Normal Form.
        private void buttonFNC_Click(object sender, EventArgs e)
        {
            string expr = textBoxInput.Text;    // expression input.

            tabControl1.SelectedIndex = 1;

            // Creates a new analyzer for the expression.
            Analyzer asl = new Analyzer(expr);
            // Tokenization phase.
            if (!Tokenize(asl)) return;
            // Parsing phase.
            AST ast = CNF.Convert(asl.Parse());
            string fnc = ASTFormat.Format(ast, ASTFormat.FormatType.PLAIN);
            CNFProposition cnf = new CNFProposition(ast);
            //IEnumerable<CnfOr> orClauses = CNF.Separate(ast, true);

            // Add this formula to the FNC list.
            RichTextTool rtt = new RichTextTool(ref richTextBoxCNF);
            rtt.AppendText(expr + " - ");
            richTextBoxCNF.SelectionBackColor = Color.Aquamarine;
            rtt.ToggleBold();
            rtt.AppendText(fnc);
            richTextBoxCNF.SelectionBackColor = Color.White;
            rtt.ToggleBold(); rtt.AppendText(" - "); rtt.ToggleBold();
            richTextBoxCNF.SelectionBackColor = Color.LawnGreen;
            rtt.AppendText(cnf.ToString());
            rtt.Eol();

            // Add the tree to the image.
            /*
            if (m_treeFiller == null) m_treeFiller = new TreeFiller(pictureBoxTree);
            m_treeFiller.Draw(ast);
            */
        }

        // Add the formula as a premisse.
        private void buttonPremisse_Click(object sender, EventArgs e)
        {
            ExtractCNFs(treeViewTheory.Nodes[0], ref m_premisses);
        }

        // Add the theorem
        private void buttonTheorem_Click(object sender, EventArgs e)
        {
            ExtractCNFs(treeViewTheory.Nodes[1], ref m_theorem, true);
        }

        // Clear the form.
        private void buttonClear_Click(object sender, EventArgs e)
        {
            atp = new ATP();
            // Clear outputs.
            pictureBoxTree.Image = null;
            richTextBoxCNF.Clear();
            richTextBoxResolution.Clear();
            m_premisses.Clear();
            m_theorem.Clear();
            treeViewTheory.Nodes[0].Nodes.Clear();
            treeViewTheory.Nodes[1].Nodes.Clear();
            // Clear errors.
            ClearErrorMsgs();
        }

        #endregion Button actions

        /// <summary>
        /// Report the proof state.
        /// </summary>
        /// <param name="message">Message from the proof.</param>
        public void Report(string message)
        {
            RichTextTool rtt = new RichTextTool(ref richTextBoxResolution);
            rtt.AppendText(message);
        }

        /// <summary>
        /// Extract the CNF formulas.
        /// </summary>
        /// <returns>An enumerable of formulas in CNF form.</returns>
        private void ExtractCNFs(TreeNode rootNode, ref List<MultipleDisjunction> data, bool andProve = false)
        {
            string expr = textBoxInput.Text;    // expression input.

            tabControl1.SelectedIndex = 2;

            // Creates a new analyzer for the expression.
            Analyzer asl = new Analyzer(expr);
            // Tokenization phase.
            if (!Tokenize(asl)) return;
            // Parsing phase.
            AST ast = CNF.Convert(asl.Parse());
            CNFProposition cnf = new CNFProposition(ast);
            string fnc = ast.ToString();
            if (andProve)
            {
                atp.Theorem = cnf;
                bool proved = atp.ProveIt(new ATP.ReportDelegate(Report));
                string msg = proved ? "TEOREMA PROVADO!" : "NÃO CONSEGUI PROVAR A TEORIA";
                MessageBoxIcon icon = proved ? MessageBoxIcon.Asterisk : MessageBoxIcon.Warning;
                MessageBox.Show(msg, "Resultado", MessageBoxButtons.OK, icon);
            }
            else
                atp.AddPremisse(cnf);

            //IEnumerable<CnfOr> orClauses = CNF.Separate(ast, true);
            IEnumerable<MultipleDisjunction> orClauses = cnf.Props;

            // Add the formula to the node and data.
            data.AddRange(orClauses);
            foreach (var clause in orClauses)
            {
                TreeNode node = new TreeNode(clause.ToString());
                rootNode.Nodes.Add(node);
            }
            rootNode.ExpandAll();
        }

        /// <summary>
        /// Tokenization Phase
        /// </summary>
        /// <param name="asl">Syntatic Analyzer</param>
        /// <returns>If tokenization succedded.</returns>
        private bool Tokenize(Analyzer asl)
        {
            if (!asl.Tokenize())
            { // There are errors from the tokenization. 
                foreach (string error in asl.Errors)
                    listBoxErrors.Items.Add(error);
                string alert = string.Format(
                    "Há {0} erros identificados. Por favor, revise-os.",
                    asl.Errors.Count());
                errorProviderInput.SetError(textBoxInput, alert);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Fill a treeview node with an AST node.
        /// </summary>
        /// <param name="ast">AST node</param>
        /// <returns>AST's TreeView node representation</returns>
        private TreeNode FillTree(AST ast)
        {
            TreeNode node, child1, child2;

            switch (ast.GetType().Name)
            {
                case "ASTProp":
                    node = new TreeNode(((ASTProp)ast).value.ToString());
                    break;
                case "ASTOpUnary":
                    node = new TreeNode(((ASTOpUnary)ast).value.ToString());
                    child1 = FillTree(((ASTOpUnary)ast).ast);
                    node.Nodes.Add(child1);
                    break;
                case "ASTOpBinary":
                    node = new TreeNode(((ASTOpBinary)ast).value.ToString());
                    child1 = FillTree(((ASTOpBinary)ast).left);
                    child2 = FillTree(((ASTOpBinary)ast).right);
                    node.Nodes.Add(child1);
                    node.Nodes.Add(child2);
                    break;
                default:
                    node = null;
                    break;
            }

            return node;
        }

        /// <summary>
        /// Clear the error messages exibition control.
        /// </summary>
        private void ClearErrorMsgs()
        {
            errorProviderInput.SetError(textBoxInput, null);
            listBoxErrors.Items.Clear();
        }
    }
}
