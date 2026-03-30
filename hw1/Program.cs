using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HW1
{
    public class Rule
    {
        public string LHS { get; set; }
        public int RHSCount { get; set; }
    }

    public class Node
    {
        public string Name { get; set; }
        public List<Node> Children { get; set; } = new List<Node>();
        public Node(string name) { Name = name; }
    }

    class Program
    {
        static List<Rule> Grammar = new List<Rule>();
        static Dictionary<int, Dictionary<string, string>> ActionTable = new Dictionary<int, Dictionary<string, string>>();
        static Dictionary<int, Dictionary<string, int>> GotoTable = new Dictionary<int, Dictionary<string, int>>();
        static HashSet<string> ValidTokens = new HashSet<string>(); 

        static void Main(string[] args)
        {
            try
            {
                LoadGrammar("Grammar.txt");
                LoadActionTable("ActionTable.txt");
                LoadGotoTable("GotoTable.txt");

                for (int i = 1; i <= 9; i++)
                {
                    ParseInput($"input{i}.txt", $"output{i}.txt");
                }
                Console.WriteLine("İşlem başarıyla tamamlandı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }

        static void ParseInput(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath)) return;

            string[] tokens = File.ReadAllText(inputPath)
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Stack<int> stateStack = new Stack<int>();
            Stack<Node> nodeStack = new Stack<Node>();
            stateStack.Push(0);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("{0,-40} {1,-40} {2}", "Stack", "Input", "Action"));
            sb.AppendLine(new string('-', 100));

            int tokenIndex = 0;
            while (tokenIndex < tokens.Length)
            {
                int currentState = stateStack.Peek();
                string currentToken = tokens[tokenIndex];

                if (!ValidTokens.Contains(currentToken) && currentToken != "$")
                {
                    File.WriteAllText(outputPath, $"Unknown token: {currentToken}");
                    return;
                }

                if (!ActionTable.ContainsKey(currentState) || !ActionTable[currentState].ContainsKey(currentToken) || ActionTable[currentState][currentToken] == "-")
                {
                    sb.AppendLine(new string('-', 100));
                    sb.AppendLine("SYNTAX ERROR at token: " + currentToken);
                    File.WriteAllText(outputPath, sb.ToString());
                    return;
                }

                string action = ActionTable[currentState][currentToken];
                string inputRemaining = string.Join(" ", tokens.Skip(tokenIndex));
                sb.AppendLine(string.Format("{0,-40} {1,-40} {2}", GetStackString(stateStack, nodeStack), inputRemaining, action));

                if (action == "accept") break;

                if (action.StartsWith("s")) 
                {
                    stateStack.Push(int.Parse(action.Substring(1)));
                    nodeStack.Push(new Node(currentToken));
                    tokenIndex++;
                }
                else if (action.StartsWith("r"))
                {
                    int ruleIdx = int.Parse(action.Substring(1)) - 1;
                    Rule rule = Grammar[ruleIdx];
                    Node parent = new Node(rule.LHS);

                    List<Node> children = new List<Node>();
                    for (int i = 0; i < rule.RHSCount; i++)
                    {
                        if (stateStack.Count > 1) stateStack.Pop();
                        if (nodeStack.Count > 0) children.Add(nodeStack.Pop());
                    }
                    children.Reverse();
                    parent.Children.AddRange(children);

                    nodeStack.Push(parent);
                    stateStack.Push(GotoTable[stateStack.Peek()][rule.LHS]);
                }
            }

            sb.AppendLine(new string('-', 100));
            sb.AppendLine("Parse tree:");
            if (nodeStack.Count > 0) PrintTree(nodeStack.Peek(), "", sb);
            File.WriteAllText(outputPath, sb.ToString());
        }

        static void PrintTree(Node node, string path, StringBuilder sb)
        {
            string currentPath = path + "/" + node.Name;
            sb.AppendLine(currentPath);
            foreach (var child in node.Children) PrintTree(child, currentPath, sb);
        }

        static string GetStackString(Stack<int> states, Stack<Node> nodes)
        {
            var sArr = states.ToArray(); Array.Reverse(sArr);
            var nArr = nodes.ToArray(); Array.Reverse(nArr);
            string res = "";
            for (int i = 0; i < sArr.Length; i++)
            {
                res += sArr[i];
                if (i < nArr.Length) res += nArr[i].Name;
            }
            return res;
        }

        static void LoadGrammar(string path)
        {
            foreach (var line in File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split(new[] { "->" }, StringSplitOptions.None);
                string lhs = parts[0].Trim().Split(' ').Last(); 
                int count = parts[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                Grammar.Add(new Rule { LHS = lhs, RHSCount = count });
            }
        }

        static void LoadActionTable(string path)
        {
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var headers = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            foreach (var h in headers) ValidTokens.Add(h);

            for (int i = 1; i < lines.Count; i++)
            {
                var row = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int state = int.Parse(row[0]);
                ActionTable[state] = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++) ActionTable[state][headers[j]] = row[j + 1];
            }
        }

        static void LoadGotoTable(string path)
        {
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var headers = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            for (int i = 1; i < lines.Count; i++)
            {
                var row = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int state = int.Parse(row[0]);
                GotoTable[state] = new Dictionary<string, int>();
                for (int j = 0; j < headers.Length; j++)
                    if (row[j + 1] != "-") GotoTable[state][headers[j]] = int.Parse(row[j + 1]);
            }
        }
    }
}