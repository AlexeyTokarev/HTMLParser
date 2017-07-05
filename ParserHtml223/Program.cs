using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Html2Markdown;

namespace ParserHtml223
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read the file as one string.
            var source = File.ReadAllText(@"Участник 44-ФЗ.html");

            // html to markdown convertor
            var markdownConverter = new Converter();


            //Create a (re-usable) parser front-end
            var parser = new HtmlParser();

            var document = parser.Parse(source);

            // result key=> question, value => answer
            var result = new Dictionary<string, string>();

            //HTML should be output in the end
            //Console.WriteLine(document.DocumentElement.OuterHtml);

            var questionElements = document.QuerySelectorAll("h3");
            foreach (var elQuestion in questionElements)
            {
                elQuestion.RemoveChild(elQuestion.LastChild);
                var answer = new StringBuilder();
                var sublingNode = elQuestion.NextSibling;
                while (sublingNode != null && !sublingNode.NodeName.Equals("h3", StringComparison.OrdinalIgnoreCase))
                {
                    var currElement = sublingNode as IElement;
                    var text = string.Empty;
                    if (currElement != null)
                    {
                        if (currElement is IHtmlTableElement)
                        {
                            text = ParseTable(currElement);
                        }
                        else
                        {
                            var tablesElements = currElement.QuerySelectorAll("table");
                            var tablesContent = string.Empty;
                            if (tablesElements != null && tablesElements.Length > 0)
                            {
                                foreach (var tbl in tablesElements)
                                {
                                    // удаляем таблицу т.к. она в основном элементе нам не нужна
                                    // будем делать отдельный парсинг
                                    sublingNode.RemoveChild(tbl);
                                    tablesContent += ParseTable(tbl);
                                }
                            }
                            text = string.Concat(markdownConverter.Convert(sublingNode.ToHtml()), tablesContent);
                        }
                    }
                    else
                    {
                        text = markdownConverter.Convert(sublingNode.ToHtml());
                    }
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (!text.Contains("Для Оператора") && !text.Contains("Действия Оператора"))
                        {
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                answer.Append(text);
                            }
                        }
                    }
                    sublingNode = sublingNode.NextSibling;
                }
                var key = elQuestion.Text();
                if (result.ContainsKey(key))
                {
                    Console.WriteLine($"Вопрос <<{key}>> уже существует");
                }
                else
                {
                    result.Add(key, answer.ToString());
                }

            }

            var csv = String.Join(
                Environment.NewLine,
                result.Select(d => d.Key + "\t" + CleareText(d.Value) + "\tEditorial")
            );
            File.WriteAllText("Вопросы в Участник 44-ФЗ.tsv", "Question\tAnswer\tSource" + Environment.NewLine + csv);

            Console.WriteLine($"Обработано {result.Count} В-О");

            Console.Read();
        }

        static string ParseTable(IElement tableElement)
        {
            return string.Empty;
        }

        private static string CleareText(string inputText)
        {
            var strRegex = @"\*\*(\s)?Ответ Оператора(\s)?:(\s)?\*\*";
            var text = inputText.Replace("\t", " ");
            text = text.Replace("\r\n", "\\n");
            text = text.Replace("\n", "\\n");
            text = text.Replace("\r", "\\n");
            text = Regex.Replace(text, strRegex, "");
            return text;
        }
    }
}
