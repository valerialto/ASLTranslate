using SpacyDotNet;
using Python.Runtime;

namespace ASLTranslate.Translator
{
    public class Translator
    {
        static string tense;

        public static List<string> Translate(string input)
        {
            Runtime.PythonDLL = "python39.dll";
            var spacy = new Spacy();
            var nlp = spacy.Load("en_core_web_sm");

            var doc = nlp.GetDocument(input);

            tense = "Fut";
            var tagwords = TimeWordsFirst(doc.Tokens);
            tagwords = NegativeLast(tagwords);
            tagwords = DeleteArticles(tagwords);
            tagwords = DeleteAux(tagwords);
            tagwords = ReverseAdjNoun(tagwords);
            tagwords = ReverseAdvVerb(tagwords);

            if (tense != "None")
            {
                var root = FindRootVerb(tagwords);
                GetTense(root);
            }

            var result = new List<string>();

            if (tense == "Past") result.Add("FINISH");
            foreach (Token token in tagwords)
            {
                result.Add(token.Lemma.ToUpper());
                if (token.Tag == "NNS" || token.Tag == "NNPS") result.Add("MULTIPLE");
            }
            if (tense == "Fut") result.Add("WILL");

            return result;
        }

        private static List<Token> TimeWordsFirst(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (token.EntType == "TIME")
                {
                    output.Add(token);
                    tense = "None";
                }
            }

            foreach (Token token in tagwords)
            {
                if (token.EntType != "TIME") output.Add(token);
            }

            return output;
        }

        private static List<Token> NegativeLast(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (token.Dep != "neg") output.Add(token);
            }

            foreach (Token token in tagwords)
            {
                if (token.Dep == "neg")
                {
                    output.Add(token);
                }
            }

            return output;
        }

        private static List<Token> DeleteArticles(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (!(token.Tag == "DT" || token.Lemma == "of" || 
                    (token.Tag == "IN" && token.Children.Exists(x => x.EntType == "TIME")))) output.Add(token);
            }

            return output;
        }

        private static List<Token> DeleteAux(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (token.PoS != "AUX") output.Add(token);
                if (token.PoS == "AUX" && (token.Dep == "Root" || token.Head.Dep == "ROOT"))
                {
                    GetTense(token);
                }
            }

            return output;
        }
        private static List<Token> ReverseAdjNoun(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (token.PoS != "ADJ") output.Add(token);
                if (token.Children.Count > 0)
                {
                    foreach (Token child in token.Children)
                    {
                        if (child.PoS == "ADJ") output.Add(child);
                    }
                }
            }

            return output;
        }

        private static List<Token> ReverseAdvVerb(List<Token> tagwords)
        {
            var output = new List<Token>();
            foreach (Token token in tagwords)
            {
                if (token.Children.Count > 0)
                {
                    foreach (Token child in token.Children)
                    {
                        if (child.PoS == "ADV" && child.Dep != "neg") output.Add(child);
                    }
                }
                if (token.PoS != "ADV" || token.Dep == "neg") output.Add(token);
            }

            return output;
        }

        private static Token FindRootVerb(List<Token> tagwords)
        {
            ; foreach (Token token in tagwords)
            {
                if (token.Dep == "ROOT")
                {
                    return token;
                }
            }

            return null;
        }

        private static void GetTense(Token token)
        {
            if (token == null) return;
            var morph = ((PyObject)token.Morph).ToList();

            foreach (PyObject s in morph)
            {
                if (s.ToString().Split('=')[0] == "Tense")
                {
                    tense = s.ToString().Split('=')[1];
                    break;
                }
            }
        }
    }
}
