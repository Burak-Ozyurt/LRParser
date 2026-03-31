using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

Console.WriteLine("LR Parser Başlatılıyor...\n");

var actionTable = new Dictionary<int, Dictionary<string, string>>();
var gotoTable = new Dictionary<int, Dictionary<string, int>>();
var grammarRules = new Dictionary<int, GrammarRule>();
var validTerminals = new HashSet<string>();

try
{
    foreach (var line in File.ReadAllLines("Grammar.txt"))
    {
        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) continue;

        int arrowIndex = Array.IndexOf(parts, "->");
        if (arrowIndex == -1) continue;

        int id = int.Parse(parts[0].Replace(".", ""));
        grammarRules[id] = new GrammarRule
        {
            Id = id,
            LeftHandSide = parts[arrowIndex - 1],
            RightHandSideCount = parts.Length - (arrowIndex + 1)
        };
    }

    var actionLines = File.ReadAllLines("ActionTable.txt");
    var actionHeaders = actionLines[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    int aStart = actionHeaders[0].ToLower() == "state" ? 1 : 0;

    for (int j = aStart; j < actionHeaders.Length; j++)
    {
        validTerminals.Add(actionHeaders[j]);
    }

    for (int i = 1; i < actionLines.Length; i++)
    {
        var parts = actionLines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;

        int state = int.Parse(parts[0]);
        actionTable[state] = new Dictionary<string, string>();

        for (int j = 1; j < parts.Length; j++)
        {
            string action = parts[j];
            if (action != "-" && action != "null" && action != "_" && action != "")
                actionTable[state][actionHeaders[aStart + j - 1]] = action;
        }
    }

    var gotoLines = File.ReadAllLines("GotoTable.txt");
    var gotoHeaders = gotoLines[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    int gStart = gotoHeaders[0].ToLower() == "state" ? 1 : 0;

    for (int i = 1; i < gotoLines.Length; i++)
    {
        var parts = gotoLines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;

        int state = int.Parse(parts[0]);
        gotoTable[state] = new Dictionary<string, int>();

        for (int j = 1; j < parts.Length; j++)
        {
            string gotoState = parts[j];
            if (gotoState != "-" && gotoState != "null" && gotoState != "_" && gotoState != "")
                gotoTable[state][gotoHeaders[gStart + j - 1]] = int.Parse(gotoState);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Dosya okuma hatası: " + ex.Message);
    return;
}

for (int i = 1; i <= 9; i++)
{
    string inputFile = $"input{i}.txt";
    string outputFile = $"output{i}.txt";

    if (!File.Exists(inputFile)) continue;

    string inputContent = File.ReadAllText(inputFile).Trim();
    string[] tokens = inputContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    using (StreamWriter writer = new StreamWriter(outputFile))
    {
        writer.WriteLine($"--- LR Parsing Trace for {inputFile} ---");
        writer.WriteLine(string.Format("{0,-30} | {1,-20} | {2}", "Stack", "Input", "Action"));
        writer.WriteLine(new string('-', 70));

        var stateStack = new List<int> { 0 };
        var symbolStack = new List<ParseTreeNode>();
        int ip = 0;
        bool isAccepted = false;

        while (true)
        {
            int currentState = stateStack.Last();
            string currentToken = ip < tokens.Length ? tokens[ip] : "$";

            string stackStr = string.Join(" ", stateStack.Select((s, index) =>
                index == 0 ? s.ToString() : $"{symbolStack[index - 1].Value} {s}"));
            string inputStr = string.Join(" ", tokens.Skip(ip));
            if (ip >= tokens.Length) inputStr = "$";

            if (!validTerminals.Contains(currentToken) && currentToken != "$")
            {
                writer.WriteLine(string.Format("{0,-30} | {1,-20} | ERROR", stackStr, inputStr));
                writer.WriteLine($"\n[HATA] Bilinmeyen Token (Unknown Token): '{currentToken}'");
                Console.WriteLine($"  -> {inputFile} Hatalı! (Unknown Token: {currentToken})");
                break;
            }

            if (!actionTable.ContainsKey(currentState) || !actionTable[currentState].ContainsKey(currentToken))
            {
                writer.WriteLine(string.Format("{0,-30} | {1,-20} | ERROR", stackStr, inputStr));
                writer.WriteLine("\n[HATA] Sözdizimi Hatası (Syntax Error)!");
                Console.WriteLine($"  -> {inputFile} Hatalı! (Syntax Error)");
                break;
            }

            string rawAction = actionTable[currentState][currentToken];
            string action = rawAction.ToUpper();

            writer.WriteLine(string.Format("{0,-30} | {1,-20} | {2}", stackStr, inputStr, rawAction));

            if (action.StartsWith("S") && action != "S")
            {
                int nextState = int.Parse(action.Substring(1));
                stateStack.Add(nextState);
                symbolStack.Add(new ParseTreeNode(currentToken));
                ip++;
            }
            else if (action.StartsWith("R") && action != "R")
            {
                int ruleId = int.Parse(action.Substring(1));
                GrammarRule rule = grammarRules[ruleId];
                ParseTreeNode parentNode = new ParseTreeNode(rule.LeftHandSide);

                for (int j = 0; j < rule.RightHandSideCount; j++)
                {
                    parentNode.Children.Insert(0, symbolStack.Last());
                    symbolStack.RemoveAt(symbolStack.Count - 1);
                    stateStack.RemoveAt(stateStack.Count - 1);
                }

                int exposedState = stateStack.Last();
                int gotoState = gotoTable[exposedState][rule.LeftHandSide];
                stateStack.Add(gotoState);
                symbolStack.Add(parentNode);
            }
            else if (action == "ACCEPT")
            {
                writer.WriteLine("\n[BAŞARILI] İfade başarıyla ayrıştırıldı!");
                Console.WriteLine($"  -> {inputFile} Başarıyla ayrıştırıldı.");
                isAccepted = true;
                break;
            }
        }

        if (isAccepted && symbolStack.Count > 0)
        {
            writer.WriteLine("\n--- Parse Tree ---");
            PrintTree(symbolStack[0], "", writer);
        }
    }
}

Console.WriteLine("\nTüm işlemler tamamlandı! Çıktıları kontrol edebilirsin.");
Console.ReadLine();


static void PrintTree(ParseTreeNode node, string indent, StreamWriter writer)
{
    if (node.Children.Count > 0)
    {
        string childrenValues = string.Join(" ", node.Children.Select(c => c.Value));
        writer.WriteLine($"{indent}{node.Value} -> {childrenValues}");

        foreach (var child in node.Children)
        {
            if (child.Children.Count > 0)
            {
                PrintTree(child, indent + "  ", writer);
            }
        }
    }
}
public class GrammarRule
{
    public int Id { get; set; }
    public string LeftHandSide { get; set; }
    public int RightHandSideCount { get; set; }
}
public class ParseTreeNode
{
    public string Value { get; set; }
    public List<ParseTreeNode> Children { get; set; }

    public ParseTreeNode(string value)
    {
        Value = value;
        Children = new List<ParseTreeNode>();
    }
}