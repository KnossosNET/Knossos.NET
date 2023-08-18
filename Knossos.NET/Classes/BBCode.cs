using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*
    All credits for this class goes to the original creator adriancs. 
    Added additional BBcode tags support and workarounded the tag inside tags not working.
    I included the original comments, credit, source link and LICENSE here.
*/

/* 
    Version 1.1
    19 November 2022
    https://github.com/adriancs2/BBCode.net

    Author: adriancs

    ---------
    Example
    ---------
    string Fields = "{videocode};{width};{height}";
    string InputSyntax = "[youtube,width={width},height={height}]{videocode}[/youtube]";
    string HtmlSyntax = "<iframe width=\"{width}\" height=\"{height}\" src=\"http://www.youtube.com/embed/{videocode}\" frameborder=\"0\" allowfullscreen></iframe>";
    string Input = "[youtube,width=230,height=340]JiosPxt_5kw[/youtube]";

    CreateCustomHtml(Input, InputSyntax, HtmlSyntax, Fields);

    ------
    Note
    -------
    1. "<" in value will be replace by "&lt;" to avoid Html Injection
*/

/*
    This is free and unencumbered software released into the public domain.

    Anyone is free to copy, modify, publish, use, compile, sell, or
    distribute this software, either in source code form or as a compiled
    binary, for any purpose, commercial or non-commercial, and by any
    means.

    In jurisdictions that recognize copyright laws, the author or authors
    of this software dedicate any and all copyright interest in the
    software to the public domain. We make this dedication for the benefit
    of the public at large and to the detriment of our heirs and
    successors. We intend this dedication to be an overt act of
    relinquishment in perpetuity of all present and future rights to this
    software under copyright law.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
    OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
    ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE.

    For more information, please refer to <https://unlicense.org>
*/

namespace BBcodes
{
	public class BBCodeRules
    {
        public string BBCodeSyntax { get; set; }
        public string HtmlSyntax { get; set; }
        public string Fields { get; set; }

        public BBCodeRules(string bBCodeSyntax, string htmlSyntax, string fields)
        {
            BBCodeSyntax = bBCodeSyntax;
            HtmlSyntax = htmlSyntax;
            Fields = fields;
        }
    }
	
    public class BBCode
    {
        static List<BBCodeRules>? _rules = null;
        static string _tempValueStr = "^````````^";
        static string _regexValue = ".+?";

        public static List<BBCodeRules> BasicRules
        {
            get
            {
                if (_rules == null)
                {
                    //https://www.bbcode.org/reference.php
                    _rules = new List<BBCodeRules>();
                    _rules.Add(new BBCodeRules("[b]","<b>",""));
                    _rules.Add(new BBCodeRules("[/b]", "</b>", ""));
                    _rules.Add(new BBCodeRules("[u]", "<u>", ""));
                    _rules.Add(new BBCodeRules("[/u]", "</u>", ""));
                    _rules.Add(new BBCodeRules("[i]", "<i>", ""));
                    _rules.Add(new BBCodeRules("[/i]", "</i>", ""));
                    _rules.Add(new BBCodeRules("[s]", "<del>", ""));
                    _rules.Add(new BBCodeRules("[/s]", "</del>", ""));

                    _rules.Add(new BBCodeRules("[br]", "<br>", ""));
                    _rules.Add(new BBCodeRules("[hr]", "<hr><br>", ""));

                    _rules.Add(new BBCodeRules("[list]", "<ul>", ""));
                    _rules.Add(new BBCodeRules("[/list]", "</ul>", ""));
                    _rules.Add(new BBCodeRules("[ul]", "<ul>", ""));
                    _rules.Add(new BBCodeRules("[/ul]", "</ul>", ""));
                    _rules.Add(new BBCodeRules("[ol]", "<ol>", ""));
                    _rules.Add(new BBCodeRules("[/ol]", "</ol>", ""));
                    _rules.Add(new BBCodeRules("[li]", "<li>", ""));
                    _rules.Add(new BBCodeRules("[/li]", "</li>", ""));

                    _rules.Add(new BBCodeRules("[center]", "<div style='text-align:center'>", ""));
                    _rules.Add(new BBCodeRules("[/center]", "</div>", ""));
                    _rules.Add(new BBCodeRules("[left]", "<div style='text-align:left'>", ""));
                    _rules.Add(new BBCodeRules("[/left]", "</div>", ""));
                    _rules.Add(new BBCodeRules("[right]", "<div style='text-align:right'>", ""));
                    _rules.Add(new BBCodeRules("[/right]", "</div>", ""));

                    _rules.Add(new BBCodeRules("[color={d1}]", "<span style='color: {d1};'>", "{d1}"));
                    _rules.Add(new BBCodeRules("[/color]", "</span>", ""));

                    _rules.Add(new BBCodeRules("[size={d1}]", "<span style='font-size: {d1}pt;'>", "{d1}"));
                    _rules.Add(new BBCodeRules("[/size]", "</span>", ""));


                    _rules.Add(new BBCodeRules("[font={d1},{d2}]", "<span style='font-family: {d1}; font-size: {d2}pt;'>", "{d1};{d2}"));
                    _rules.Add(new BBCodeRules("[font={d1}]", "<span style='font-family: {d1};'>", "{d1}"));
                    _rules.Add(new BBCodeRules("[/font]", "</span>", ""));

                    _rules.Add(new BBCodeRules("[code]", "<pre>", ""));
                    _rules.Add(new BBCodeRules("[/code]", "</pre>", ""));

                    _rules.Add(new BBCodeRules("[quote]", "<blockquote>", ""));
                    _rules.Add(new BBCodeRules("[quote={name}]", "<p><b>{name}</b></p><blockquote>", "{name}"));
                    _rules.Add(new BBCodeRules("[/quote]", "</blockquote>", ""));

                    //Need to have the http:// or https:// in the address in order to work
                    _rules.Add(new BBCodeRules("[url]{text}[/url]", "<a href='{text}'>{text}</a>", "{text}"));
                    _rules.Add(new BBCodeRules("[url={d1}]", "<a href='{d1}'>", "{d1}"));
                    _rules.Add(new BBCodeRules("[/url]", "</a>", ""));

                    _rules.Add(new BBCodeRules("[img]", "<img src='", ""));
                    _rules.Add(new BBCodeRules("[img={width}x{height}]", "<img style='width: {width}px; height: {height}px;' src='", "{width};{height}"));
                    _rules.Add(new BBCodeRules("[/img]", "' />", ""));

                    //Not supported by html renderer
                    /*
                    _rules.Add(new BBCodeRules("[table]", "<table>", ""));
                    _rules.Add(new BBCodeRules("[/table]", "</table>", ""));
                    _rules.Add(new BBCodeRules("[tr]", "<tr>", ""));
                    _rules.Add(new BBCodeRules("[/tr]", "</tr>", ""));
                    _rules.Add(new BBCodeRules("[th]", "<th>", ""));
                    _rules.Add(new BBCodeRules("[/th]", "</th>", ""));
                    _rules.Add(new BBCodeRules("[td]", "<td>", ""));
                    _rules.Add(new BBCodeRules("[/td]", "</td>", ""));
                    
                    _rules.Add(new BBCodeRules("[youtube,{width},{height}]{videocode}[/youtube]", "<iframe width=\"{width}\" height=\"{height}\" src=\"https://www.youtube.com/embed/{videocode}\" title=\"YouTube video player\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>", "{videocode};{width};{height}"));
                    _rules.Add(new BBCodeRules("[youtube]{videocode}[/youtube]", "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{videocode}\" title=\"YouTube video player\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>", "{videocode}"));
                    */
                }
                return _rules;
            }
            set
            {
                _rules = value;
            }
        }

        public static string ConvertToHtml(string input)
        {
            return ConvertToHtml(input, BasicRules);
        }

        public static string ConvertToHtml(string input, List<BBCodeRules> rules)
        {
            return ConvertToHtml(input, rules, false);
        }

        public static string ConvertToHtml(string input, List<BBCodeRules> rules, bool allowHtmlScriptTagsAsValue)
        {
            foreach (var r in rules)
            {
                string inputSyntax = r.BBCodeSyntax;
                string htmlSyntax = r.HtmlSyntax;
                string fields = r.Fields;

                string[] _fields = fields.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < _fields.Length; i++)
                {
                    _fields[i] = _fields[i].Trim().ToLower();
                }

                string tempInputSyntax = inputSyntax;
                foreach (string field in _fields)
                {
                    tempInputSyntax = tempInputSyntax.Replace(field, _tempValueStr);
                }
                tempInputSyntax = EsceapeForRegex(tempInputSyntax);
                tempInputSyntax = tempInputSyntax.Replace(_tempValueStr, _regexValue);
                MatchCollection mc = Regex.Matches(input, tempInputSyntax, RegexOptions.IgnoreCase);

                foreach (Match m in mc)
                {
                    string customInsertPart = m.Value;
                    string html = BuildHtml(customInsertPart, inputSyntax, htmlSyntax, _fields, allowHtmlScriptTagsAsValue);
                    input = input.Replace(customInsertPart, html);
                }
            }

            //Disabled this, the return is implemented via a html style
            //input = input.Replace("\r\n", "<br />");
            //input = input.Replace("\r", "<br />");
            //input = input.Replace("\n", "<br />");
            input = input.Replace("[*]", "<br />");
            return input;
        }

        static string BuildHtml(string extractedBBCodeInput, string inputSyntax, string htmlSyntax, string[] _fields, bool allowHtmlScriptTagsAsValue)
        {
            string customPart = extractedBBCodeInput;

            #region Dictionaries

            // Store the index of fields
            Dictionary<int, string> _idxFields = new Dictionary<int, string>();

            // Store the index of values
            Dictionary<int, string> _idxValues = new Dictionary<int, string>();

            // Store non field index & length from OriInputText
            Dictionary<int, int> _idxNonFieldLength = new Dictionary<int, int>();
            #endregion

            #region Study the structure of BBCode, Retrieve block index of non-Field and Field
            string oriInputSyntax = inputSyntax;

            // Replace all fields with temporary string
            foreach (string a in _fields)
            {
                oriInputSyntax = oriInputSyntax.Replace(a, _tempValueStr);
            }

            // Get non Field Text Array
            string[] nonFieldArray = oriInputSyntax.Split(new string[] { _tempValueStr }, StringSplitOptions.RemoveEmptyEntries);

            // Get non Field Text's Block Length
            for (int i = 0; i < nonFieldArray.Length; i++)
            {
                _idxNonFieldLength[i] = nonFieldArray[i].Length;
            }

            // Get Field index
            for (int i = 0; i < nonFieldArray.Length; i++)
            {
                // Remove non Field Block
                inputSyntax = inputSyntax.Substring(nonFieldArray[i].Length, inputSyntax.Length - nonFieldArray[i].Length);

                // Get Field index
                foreach (string s in _fields)
                {
                    if (inputSyntax.Length < s.Length)
                        break;

                    // Calculate the Field's Length
                    string b = inputSyntax.Substring(0, s.Length);

                    // Check, if the current field's name
                    // If match
                    if (b == s)
                    {
                        // Add the field and index into dictionary
                        _idxFields[i] = b;

                        // Remove field from inputSyntax
                        inputSyntax = inputSyntax.Substring(s.Length, inputSyntax.Length - s.Length);
                        break;
                    }
                }
            }
            #endregion

            #region Extract Values
            // Remove Non Field Text
            for (int i = 0; i < nonFieldArray.Length; i++)
            {
                // Remove non field block
                customPart = customPart.Substring(nonFieldArray[i].Length, customPart.Length - nonFieldArray[i].Length);

                // Current non-field block is the last block
                // Terminate the loop.
                // No more value block should exist after last block
                if (i + 1 >= nonFieldArray.Length)
                    break;

                // Detect next non-field block and calculate value length
                int v = customPart.IndexOf(nonFieldArray[i + 1]);

                // Get the index and value into dictionary
                _idxValues[i] = customPart.Substring(0, v);

                // Remove the added value from input text
                customPart = customPart.Substring(v, customPart.Length - v);
            }
            #endregion

            #region Prevent Html/Script Injection
            // Avoid Html/Script Injection, Abondon Html Conversion if detected
            if (!allowHtmlScriptTagsAsValue)
            {
                foreach (KeyValuePair<int, string> kv in _idxValues)
                {
                    bool portentialScriptExists = false;
                    if (kv.Value.Contains("<") || kv.Value.Contains("&lt;"))
                    {
                        _idxValues[kv.Key] = "";
                        portentialScriptExists = true; ;
                    }
                    if (portentialScriptExists)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int n = 0; n < nonFieldArray.Length; n++)
                        {
                            sb.Append(nonFieldArray[n]);
                            if (_idxFields.ContainsKey(n))
                                sb.Append(_idxValues[n]);
                        }
                        return sb.ToString();
                    }
                }
            }
            #endregion

            #region Fill the Values into HtmlSyntax
            // Store the value to the field
            foreach (KeyValuePair<int, string> kv in _idxFields)
            {
                // Replace the field in html with value
                htmlSyntax = htmlSyntax.Replace(kv.Value, _idxValues[kv.Key].Replace("<", "&lt;").Replace("\"", "&#34;").Replace("'", "&#39;"));
            }
            #endregion

            return htmlSyntax;
        }

        static string EsceapeForRegex(string inputSyntax)
        {
            return inputSyntax.Replace("\\", "\\\\")
                    .Replace(".", "\\.")
                    .Replace("{", "\\{")
                    .Replace("}", "\\}")
                    .Replace("[", "\\[")
                    .Replace("]", "\\]")
                    .Replace("+", "\\+")
                    .Replace("$", "\\$")
                    .Replace(" ", "\\s")
                    .Replace("#", "[0-9]")
                    .Replace("?", ".")
                    .Replace("*", "\\w*")
                    .Replace("%", ".*");
        }

        public static string AllowTags(string html, string allowedTags)
        {
            if (allowedTags == null || allowedTags.Trim() == "")
                return html;

            string[] sa = allowedTags.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            html = html.Replace("<", "&lt;");

            foreach (string s in sa)
            {
                html = Regex.Replace(html, "&lt;" + s, "<" + s, RegexOptions.IgnoreCase);
                html = Regex.Replace(html, "&lt;/" + s, "</" + s, RegexOptions.IgnoreCase);
            }
            return html;
        }

        public static string BlockTags(string html, string blockedTags)
        {
            string[] sa = blockedTags.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in sa)
            {
                html = Regex.Replace(html, "</\\W{0,}" + s, "&lt;/" + s, RegexOptions.IgnoreCase);
                html = Regex.Replace(html, "<\\W{0,}" + s, "&lt;" + s, RegexOptions.IgnoreCase);
            }
            return html;
        }
    }
}
