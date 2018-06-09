/* 
 * DelimitedTextCleaner: C# class for parsing and scrubbing a line of CSV or other 
 * similar delimited text
 * 
 * Author: Megan Brooks
 * Company: Sql2Go (Sql2go.com), Sacramento, California, USA, May, 2017
 * Inspired by https://github.com/JaGTM/CSVFixer
 * ...but is a complete rewrite
 * Based loosely on RFC 4180 (https://tools.ietf.org/html/rfc4180)
 * License: Released as free software under the MIT License (below)
 ******************************************************************************************
 Copyright 2017 Megan Brooks

 Permission is hereby granted, free of charge, to any person obtaining a copy of this
 software and associated documentation files (the "Software"), to deal in the Software
 without restriction, including without limitation the rights to use, copy, modify, merge,
 publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
 persons to whom the Software is furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all copies
 or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 DEALINGS IN THE SOFTWARE.
******************************************************************************************
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sql2Go.DelimitedTextCleaner
{
    /// <summary>
    /// Accepts a line of valid or invalid delimited text and produces either corrected
    /// delimited text or an array of corrected field values.Intended for use in an MS 
    /// SSIS script task, but contains no references specific to MS SSIS. Uses double
    /// quotes to escape embedded delimiters and double quotes, and removes unnecessary
    /// double quotes from the output.
    /// </summary>
    public class Cleaner
    {
        private class FieldInfo                         //Internal dictionary storage for each field
        {
            public string fieldChars;                   //The field data itself (quotes are NOT doubled)
            public bool requiresQuotes;                 //True if field requires enclosing quotes
            public bool hasDamagedQuotes;               //True if damaged quotes in field

            public FieldInfo()
            {
                requiresQuotes = false;
            }

            public FieldInfo(string FieldChars, bool RequiresQuotes)
            {
                fieldChars = FieldChars;
                requiresQuotes = RequiresQuotes;
            }
        }

        /*
         * There is more than one way that invalid quotes can be interpreted 
         * and corrected. This code, as released, will replace invalid double quote 
         * characters with valid ones. In effect, an incoming string of double quote 
         * characters of odd length where it does not belong will be expanded by one 
         * double quote character. In fields containing invalid double quotes, it will
         * also force recognition of field delimiters irrespective of quoting.
         * 
         * Employs a simple state machine design that can be customized as desired. Each 
         * incoming character is evaluated in the context of the current state, and may 
         * trigger a switch to a different state, with side effects.
         * .
         */

        private readonly char fieldDelimiter = ',';     //Parameter, comma by default
        private readonly bool hasHeaderRow = true;      //Parameter, true if data has header row
        private readonly char textDelimiter = '"';      //Might become a parameter someday

        private readonly char CR = '\r';                //EOL -- constant
        private readonly char LF = '\n';                //Newline -- constant
        private readonly char[] validCtl = new char[] { '\r', '\n', '\t' }; //Ctl chars we will quote

        private bool firstLineWasParsed = false;        //True once first line parsed
        private int fieldCount = 0;                     //Number of header fields found

        private ParserState parserState;                //Current parser state
        private bool EndOfLineReached;                  //True after logical EOL found
        private int charIndex;                          //Current position in line buffer
        private int quotesInARow;                       //Multiple quote counter
        private char thisChar = '\0';                   //Current character
        private FieldInfo thisFieldInfo;                //Current field
        private List<FieldInfo> headerList;             //List of all header field names
        private List<FieldInfo> fieldList;              //List of all fields in line
        private StringBuilder fieldChars;               //Cleaned characters
        private bool damagedQuoteFound;                 //True if field contains damaged quote(s)
        private bool anyDamagedQuoteFound;              //True if damage in any field

        private string[] headers;                       //Array of headers, if defined
        private Dictionary<string, int> headerIndexes;  //Dictionary of header index values
        private string[] fields;                        //Array of field values;

        private enum ParserState
        {
            AtDelimiter = 0,                            //Hit delimiter, or at start
            InText,                                     //In text without quotes
            QuoteInText,                                //Hit double quote in text
            InQuotedText,                               //Somewhere in quoted text
            QuoteInQuotedText,                          //Finished quoted text (maybe)
            AtEOL                                       //Reach end of line
        }

        /// <summary>
        /// Default constructor -- assume comma delimiter, has header row
        /// </summary>
        public Cleaner()
        {
        }

        /// <summary>
        /// Specify delimiter character and whether column names are present
        /// </summary>
        /// <param name="Delimiter">Field delimiter character</param>
        /// <param name="HasHeaderRow">True if first row presented contains column names</param>
        public Cleaner(char Delimiter, bool HasHeaderRow)
        {
            Debug.Assert(Delimiter != textDelimiter);
            fieldDelimiter = Delimiter;
            hasHeaderRow = HasHeaderRow;
        }

        /// <summary>
        /// Clean up delimited text
        /// </summary>
        /// <param name="currentLine">Delimited text to clean</param>
        public bool CleanText(string currentLine)
        {
            int currentChar = 0;
            return CleanText(currentLine, ref currentChar);
        }

        /// <summary>
        /// Clean up multi-line delimited text
        /// </summary>
        /// <param name="currentLine">Delimited text to clean</param>
        /// <param name="currentChar">Index of next character, set to -1 when buffer end reached</param>
        public bool CleanText(string currentLine, ref int currentChar)
        {
            charIndex = currentChar - 1;                    //Current index in line
            parserState = ParserState.AtDelimiter;          //Current parser state
            fieldList = new List<FieldInfo>();   //List of clean field values
            fieldChars = new StringBuilder(currentLine.Length); //Field being cleaned
            quotesInARow = 0;                               //Multiple quote counter
            thisFieldInfo = null;                           //No fields yet
            damagedQuoteFound = false;                      //No damage, yet
            anyDamagedQuoteFound = false;

            while (++charIndex < currentLine.Length)        //Next character
            {
                if (parserState == ParserState.AtEOL)       //Finish up if EOL
                {
                    var newChar = currentLine[charIndex];   //Current character

                    if (thisChar == CR && newChar == LF)
                        charIndex++;                        //Flush past LF

                    break;                                  //Drop out to EOL processing
                }

                thisChar = currentLine[charIndex];          //Current character

                if (thisChar < ' ' && !validCtl.Contains(thisChar))
                    continue;                               //Flush weird chars

                if (thisFieldInfo == null)                  //Check start of new field
                    thisFieldInfo = new FieldInfo(null, false);

                if (thisChar == textDelimiter)              //Encountered double quote
                {
                    switch (parserState)
                    {
                        case ParserState.AtDelimiter:       //Opening double quote (don't count)
                            parserState = ParserState.InQuotedText;
                            break;                          //Don't count or queue
                        case ParserState.InText:            //In unquoted text
                            MarkQuoteError();               //That's not right
                            parserState = ParserState.QuoteInText;
                            quotesInARow++;                 //Count quotes
                            break;
                        case ParserState.QuoteInText:       //Already at quote in unquoted text
                            quotesInARow++;                 //Count quotes
                            break;
                        case ParserState.InQuotedText:      //In quoted text
                            parserState = ParserState.QuoteInQuotedText;
                            quotesInARow++;                 //Count quotes
                            break;
                        case ParserState.QuoteInQuotedText:
                            quotesInARow++;                 //Count quotes
                            break;
                        default:
                            Debug.Assert(false);            //Where did I go wrong?
                            break;
                    }
                }
                else if (thisChar == fieldDelimiter)        //Hit field delimiter
                {
                    switch (parserState)                    //Treat as text if quoted
                    {
                        case ParserState.AtDelimiter:       //Early field delimiter
                        case ParserState.InText:            //End of text-only field
                            EndOfField();
                            break;
                        case ParserState.InQuotedText:      //Field delimiter inside quotes
                            if (!damagedQuoteFound)
                                SaveChar();                 //Normally is escaped
                            else
                                EndOfField();               //Safe mode -- bad quotes
                            break;
                        case ParserState.QuoteInQuotedText: //Delimiter after quote(s)
                            if (FlushQuotes())              //If normal quoted field close
                                EndOfField();               //Dangler is expected and ignored
                            else if (!damagedQuoteFound)
                                SaveChar();                 //Delimiter is escaped
                            else
                                EndOfField();               //Safe mode -- bad quotes
                            break;
                        case ParserState.QuoteInText:       //Delimiter after quote in text
                            if (FlushQuotes())               //Flush quotes at end of field
                                SaveChar(textDelimiter);    //Add extra if dangler

                            EndOfField();
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                else if (thisChar == CR || thisChar == LF)      //Logical EOL
                {
                    switch (parserState)
                    {
                        case ParserState.QuoteInText:
                            if (FlushQuotes())                  //Flush quotes at end of field
                                SaveChar(textDelimiter);        //Add extra if dangler

                            EndOfField();
                            parserState = ParserState.AtEOL;    //Trigger exit
                            break;
                        case ParserState.InQuotedText:
                            if (!damagedQuoteFound)
                                SaveChar();                     //Normally is escaped
                            else
                            {
                                EndOfField();                   //Safe mode -- bad quotes
                                parserState = ParserState.AtEOL;    //Trigger exit
                            }
                            break;
                        case ParserState.QuoteInQuotedText:
                            FlushQuotes();                      //Quoted field close
                            EndOfField();
                            parserState = ParserState.AtEOL;    //Trigger exit
                            break;
                        default:
                            EndOfField();                       //It's done, whatever it was
                            parserState = ParserState.AtEOL;    //Trigger exit
                            break;
                    }

                    quotesInARow = 0;
                }
                else                                        //Nothing special
                {
                    switch (parserState)
                    {
                        case ParserState.AtDelimiter:
                            parserState = ParserState.InText;
                            SaveChar();
                            break;
                        case ParserState.QuoteInText:
                            if (FlushQuotes())
                                SaveChar(textDelimiter);    //Add extra for dangler

                            SaveChar(ParserState.InText);   //Error was already flagged
                            break;
                        case ParserState.QuoteInQuotedText:
                            if (FlushQuotes())
                            {
                                SaveChar(textDelimiter);    //Add extra if dangler
                                MarkQuoteError();           //That shouldn't be
                            }

                            SaveChar(ParserState.InQuotedText); //Normal char following escaped quotes
                            break;
                        default:
                            SaveChar();                     //A very ordinary character
                            break;
                    }

                    quotesInARow = 0;
                }
            }

            switch (parserState)                    //Finish final field
            {
                case ParserState.AtDelimiter:       //Trailing delimiter
                    thisFieldInfo = new FieldInfo(null, false); //Create empty field
                    break;
                case ParserState.InQuotedText:      //EOL inside quotes
                    MarkQuoteError();               //Closing quote missing
                    break;
                case ParserState.QuoteInQuotedText: //EOL after quote(s)
                    if (FlushQuotes())              //If normal quoted field close
                        break;                      //Dangler is expected and ignored

                    MarkQuoteError();               //Closing quote missing
                    break;
                case ParserState.QuoteInText:
                    if (FlushQuotes())              //Already an error, just flush
                        SaveChar(textDelimiter);    //Extra quote if dangler
                    break;
                default:                            //No quote, no problem
                    break;
            }

            EndOfField();                                   //Process final field end

            if (!firstLineWasParsed)                        //First line only
            {
                headerIndexes = null;                       //Clearn index cache
                headers = null;                             //Clear header cache

                if (hasHeaderRow)                           //Save header list
                    headerList = fieldList;

                firstLineWasParsed = true;
            }

            fields = null;                                  //Clear field values cache

            if (charIndex < currentLine.Length)             //Buffer is not exhausted
                currentChar = charIndex;                    //Return resume point
            else
                currentChar = -1;                           //Buffer is exhausted

            return !anyDamagedQuoteFound;
        }

        private void SaveChar()                             // Save field character
        {
            SaveChar(thisChar);
        }

        private void SaveChar(char chr) // Save specified character and flag for escaping if needed
        {
            if (chr == textDelimiter
                || chr == fieldDelimiter 
                || thisChar < ' ')
            {
                thisFieldInfo.requiresQuotes = true;
            }

            fieldChars.Append(chr);
        }

        private void SaveChar(ParserState newState) // Save field character and change state
        {
            parserState = newState;
            SaveChar();
        }

        private bool FlushQuotes()  // Save accumulated quote characters
        {
            bool oddCount;

            if (quotesInARow != 0)
            { 
                oddCount = (quotesInARow & 1) == 1;     //Check for dangler
                int pairCount = quotesInARow / 2;       //Count valid pairs
                quotesInARow = 0;

                while (pairCount-- > 0)                 //Save valid quotes
                {
                    SaveChar(textDelimiter);            //Unescaped double quote
                }
            }
            else
            {
                oddCount = false;
            }

            return oddCount;
        }

        private void EndOfField()                           // Process end of field
        {
            if (thisFieldInfo != null)                      //Add field to list if started
            {
                if (!firstLineWasParsed)                    //First line only
                    fieldCount++;                           //Count fields

                thisFieldInfo.fieldChars = fieldChars.ToString();   //Save characters
                fieldChars.Clear();                         //Reset stringbuilder for next field
                fieldList.Add(thisFieldInfo);               //Save field
                thisFieldInfo = null;                       //Reset field for next
            }

            if (parserState != ParserState.AtEOL)           //No final delim if at EOL
                parserState = ParserState.AtDelimiter;      //Back to initial state

            quotesInARow = 0;                               //Make sure quote count is reset
            damagedQuoteFound = false;                      //And damage indicator
        }

        private void MarkQuoteError()
        {
            damagedQuoteFound = true;   //Alter parsing to protect delimiters from bad quotes
            anyDamagedQuoteFound = true;
            thisFieldInfo.hasDamagedQuotes = true;          //Remember where it happened
        }

        /// <summary>
        /// Add or consolidate fields to match expected count
        /// </summary>
        /// <returns>0 = no change, -1 = empty columns added, 1 = extra columns consolidated</returns>
        public int ReconcileFieldCount()
        {
            Debug.Assert(firstLineWasParsed);

            int actualCount = fieldList.Count();

            if (actualCount < fieldCount)
            {
                int shortage = fieldCount - actualCount;

                while (shortage-- > 0)
                {
                    thisFieldInfo = new FieldInfo(string.Empty, false);
                    fieldList.Add(thisFieldInfo);
                }

                return -1;
            }
            else if (actualCount > fieldCount)
            {
                string composite = String.Join(
                    fieldDelimiter.ToString(),
                    (from item in fieldList.Skip(fieldCount - 1)    //Merge extras into last valid col
                     select item.fieldChars)
                );

                fieldList[fieldCount - 1] =             //Update final valid field
                    new FieldInfo(composite, true);

                fieldList =                             //Strip extra fields
                    new List<FieldInfo>(fieldList.Take(fieldCount));

                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Return line as delimited string, escaped as required and with unnecessary quotes removed
        /// </summary>
        /// <returns>Escaped string</returns>
        public string ReturnText()
        {
            return ReturnText(false);
        }

        /// <summary>
        /// Return line as delimited string, escaped as required and 
        /// optionally with enclosing quotes
        /// </summary>
        /// <returns>Escaped string</returns>
        public string ReturnText(bool alwaysEnclose)
        {
            var textDelimiterString = textDelimiter.ToString(); //char to string

            return string.Join(
                fieldDelimiter.ToString(),                       //Field delimiter
                (from item in fieldList
                 select item.requiresQuotes || alwaysEnclose
                    ? String.Concat(                            //All fields quoted
                            textDelimiterString,                //Opening quote
                            item.fieldChars.Replace(            //Also double embedded quotes
                                textDelimiterString,            //Search for one
                                string.Concat(                  //Replace with two
                                    textDelimiterString,
                                    textDelimiterString)),
                            textDelimiterString)                //Closing quote
                    : item.fieldChars)
           );
        }

        /// <summary>
        /// Return indivdual field names without enclosing quotes or escapes
        /// </summary>
        /// <returns>String array of field values</returns>
        public string[] ReturnHeaders()
        {
            if (headers != null)
            {
                return headers;
            }
            else if (headerList != null)
            {
                headers = (
                    from item in headerList
                    select item.fieldChars
                ).ToArray();

                return headers;
            }
            else
            {
                headers = new string[] { };
                return headers;
            }
        }

        /// <summary>
        /// Return indivdual field values without enclosing quotes or escapes
        /// </summary>
        /// <returns>String array of field names</returns>
        public string[] ReturnFields()
        {
            if (fields != null)
            {
                return fields;
            }
            else if (fieldList != null)
            { 
                fields = (
                    from item in fieldList
                    select item.fieldChars
                ).ToArray();

                return fields;
            }
            else
            {
                fields = new string[] { };
                return fields;
            }
        }

        /// <summary>
        /// Return current value of named field
        /// </summary>
        /// <param name="fieldName">Name of field to return value</param>
        /// <returns>Field value if field name exists, else null</returns>
        public string this[string fieldName]
        {
            get
            {
                if (headerIndexes == null)                  //If dict not yet built
                {
                    headerIndexes = new Dictionary<string, int>();
                    var h = ReturnHeaders();                //Get header array ptr

                    for (int i = 0; i < h.Length; i++)
                    {
                        if (!headerIndexes.ContainsKey(h[i]))   //1st instance only
                            headerIndexes.Add(h[i], i);     //Add header index to dict
                    }
                }

                int fi;

                if (headerIndexes.TryGetValue(fieldName, out fi))   //Look up index
                    return ReturnFields()[fi];              //Return value if found
                else
                    return null;
            }
        }
    }
}
