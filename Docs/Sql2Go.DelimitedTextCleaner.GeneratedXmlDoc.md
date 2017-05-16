# Sql2Go.DelimitedTextCleaner #

## Type Cleaner

 Accepts a line of valid or invalid delimited text and produces either corrected delimited text or an array of corrected field values.Intended for use in an MS SSIS script task, but contains no references specific to MS SSIS. Uses double quotes to escape embedded delimiters and double quotes, and removes unnecessary double quotes from the output. 



---
#### Field Cleaner.fieldDelimiter

 If set, invalid double quotes will not cause an error return from CleanText 



---
#### Method Cleaner.#ctor

 Default constructor -- assume comma delimiter, has header row 



---
#### Method Cleaner.#ctor(System.Char,System.Boolean)

 Specify delimiter character 

|Name | Description |
|-----|------|
|Delimiter: |Field delimiter character|
|HasHeaderRow: |True if first row presented contains column names|


---
#### Method Cleaner.CleanText(System.String)

 Clean up delimited text 

|Name | Description |
|-----|------|
|currentLine: |Delimited text to lean|


---
#### Method Cleaner.SaveChar

 Save field character 



---
#### Method Cleaner.SaveChar(System.Char)

 Save specified character and flag for escaping if needed 

|Name | Description |
|-----|------|
|chr: ||


---
#### Method Cleaner.SaveChar(Sql2Go.DelimitedTextCleaner.Cleaner.ParserState)

 Save field character and change state 



---
#### Method Cleaner.FlushQuotes

 Save accumulated quote characters 

**Returns**: 



---
#### Method Cleaner.EndOfField

 Process end of field 



---
#### Method Cleaner.ReconcileFieldCount

 Add or consolidate fields to match expected count 

**Returns**: 0 = no change, -1 = empty columns added, 1 = extra columns consolidated



---
#### Method Cleaner.ReturnText

 Return line as delimited string, escaped as required and with unnecessary quotes removed 

**Returns**: Escaped string



---
#### Method Cleaner.ReturnText(System.Boolean)

 Return line as delimited string, escaped as required and optionally with enclosing quotes 

**Returns**: Escaped string



---
#### Method Cleaner.ReturnHeaders

 Return indivdual field names without enclosing quotes or escapes 

**Returns**: String array of field values



---
#### Method Cleaner.ReturnFields

 Return indivdual field values without enclosing quotes or escapes 

**Returns**: String array of field names



---


