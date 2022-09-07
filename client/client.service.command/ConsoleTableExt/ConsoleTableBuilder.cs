using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConsoleTableExt
{
    public class ConsoleTableBaseData
    {
        public List<object> Column { get; set; }
        public List<List<object>> Rows { get; set; }
    }

    public class ConsoleTableBuilder
    {
        internal List<object> Column { get; set; }
        internal List<string> FormattedColumns { get; set; }

        internal List<List<object>> Rows { get; set; }
        internal List<List<object>> FormattedRows { get; set; }

        internal ConsoleTableBuilderFormat TableFormat { get; set; }
        internal Dictionary<CharMapPositions, char> CharMapPositionStore { get; set; } = null;
        internal Dictionary<HeaderCharMapPositions, char> HeaderCharMapPositionStore { get; set; } = null;
        internal List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>> TopMetadataRows = new List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>>();
        internal List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>> BottomMetadataRows = new List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>>();
        internal Dictionary<int, TextAligntment> TextAligmentData = new Dictionary<int, TextAligntment>();
        internal Dictionary<int, TextAligntment> HeaderTextAligmentData = new Dictionary<int, TextAligntment>();
        internal Dictionary<int, int> MinLengthData = new Dictionary<int, int>();

        internal bool CanTrimColumn = false;
        internal string TableTitle = string.Empty;
        internal TextAligntment TableTitleTextAlignment = TextAligntment.Center;
        internal ConsoleColorNullable TableTitleColor = new ConsoleColorNullable();
        internal string PaddingLeft = " ";
        internal string PaddingRight = " ";

        internal int TitlePositionStartAt { get; set; }
        internal int TitlePositionLength { get; set; }

        internal Dictionary<int, Func<object, string>> FormatterStore = new Dictionary<int, Func<object, string>>();
        internal Dictionary<int, Func<string, string>> ColumnFormatterStore = new Dictionary<int, Func<string, string>>();

        private ConsoleTableBuilder()
        {
            Column = new List<object>();
            Rows = new List<List<object>>();
            TableFormat = ConsoleTableBuilderFormat.Default;
        }

        /// <summary>
        /// This function allow developer implement themeself data-source
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static ConsoleTableBuilder From(Func<ConsoleTableBaseData> func)
        {
            if (func != null)
            {
                var baseData = func.Invoke();
                return From(baseData);
            }
            else
            {
                throw new Exception("invaild function");
            }
        }

        public static ConsoleTableBuilder From(ConsoleTableBaseData baseData)
        {
            var builder = new ConsoleTableBuilder();

            if (baseData != null)
            {
                if (baseData.Rows != null)
                {
                    builder.Rows = baseData.Rows;
                }

                if (baseData.Column != null)
                {
                    builder.Column = baseData.Column;
                }
            }

            return builder;
        }

        public static ConsoleTableBuilder From(List<int> list)
        {
            var builder = new ConsoleTableBuilder();
            foreach (var value in list)
            {
                builder.Rows.Add(new List<object> { value });
            }

            return builder;
        }

        public static ConsoleTableBuilder From(List<string> list)
        {
            var builder = new ConsoleTableBuilder();
            foreach (var value in list)
            {
                builder.Rows.Add(new List<object> { value });
            }

            return builder;
        }

        public static ConsoleTableBuilder From(List<object> list)
        {
            var builder = new ConsoleTableBuilder();

            foreach (var value in list)
            {
                builder.Rows.Add(new List<object> { value });
            }

            return builder;
        }

        public static ConsoleTableBuilder From(DataTable dt)
        {
            var builder = new ConsoleTableBuilder();

            if (dt == null)
            {
                return builder;
            }

            var columnNames = dt.Columns.Cast<DataColumn>()
                .Select(x => x.ColumnName)
                .ToList();
#if NET35
            columnNames.ForEach(f => builder.Column.Add(f));
#else

            builder.Column = new List<object>(columnNames);
#endif
            foreach (DataRow row in dt.Rows)
            {
                builder.Rows.Add(new List<object>(row.ItemArray));
            }

            return builder;
        }

        public static ConsoleTableBuilder From<T>(List<T> list) where T : class
        {
            var builder = new ConsoleTableBuilder();
            if (list == null)
            {
                return builder;
            }

            var isClass = typeof(T).IsClass;
            var props = new List<System.Reflection.PropertyInfo>();

            if (list.Any())
            {
                props = list.First().GetType().GetProperties().ToList();
            }

            List<object> columnNames;
            if (isClass)
            {
                columnNames = props.Select(p =>
                {
                    object[] attrs = p.GetCustomAttributes(true);
                    foreach (object attr in attrs)
                    {
                        if (attr is System.ComponentModel.DescriptionAttribute)
                        {
                            return ((System.ComponentModel.DescriptionAttribute)attr).Description;
                        }
                    }

                    return p.Name as object;
                }).ToList() ?? new List<object>();
            }
            else
            {
                columnNames = new List<object> { "Value" };
            }


            builder.Column = columnNames;

            foreach (var item in list)
            {
                if (isClass == true)
                {
                    var itemPropValues = new List<object>();

                    foreach (var prop in props)
                    {
#if NET35
                                    var objValue = prop.GetValue(item, new object[]{ });
#else
                        var objValue = prop.GetValue(item);
#endif
                        itemPropValues.Add(objValue);
                    }

                    builder.Rows.Add(itemPropValues);
                }
                else
                {
                    builder.Rows.Add(new List<object> { item });
                }
            }

            return builder;
        }

        public static ConsoleTableBuilder From(List<object[]> rows)
        {
            var builder = new ConsoleTableBuilder();

            if (rows == null)
            {
                return builder;
            }

            foreach (var row in rows)
            {
                builder.Rows.Add(new List<object>(row));
            }

            return builder;
        }

        public static ConsoleTableBuilder From(List<List<object>> rows)
        {
            var builder = new ConsoleTableBuilder();

            if (rows == null)
            {
                return builder;
            }

            foreach (var row in rows)
            {
                builder.Rows.Add(row);
            }

            return builder;
        }

        internal void PopulateFormattedColumnsRows()
        {
            FormattedColumns = Enumerable.Range(0, Column.Count)
                .Select(idx =>
                {
                    if (ColumnFormatterStore.ContainsKey(idx))
                    {
                        return ColumnFormatterStore[idx](Column[idx] == null ? string.Empty : Column[idx].ToString());
                    }
                    else
                    {
                        return Column[idx] == null ? string.Empty : Column[idx].ToString();
                    }
                }).ToList();


            FormattedRows = new List<List<object>>();
            for (int i = 0; i < Rows.Count; i++)
            {
                FormattedRows.Add(
                    Enumerable.Range(0, Rows[i].Count)
                    .Select(idx =>
                    {
                        if (FormatterStore.ContainsKey(idx))
                        {
                            return FormatterStore[idx](Rows[i][idx] == null ? string.Empty : Rows[i][idx]);
                        }
                        else
                        {
                            return Rows[i][idx];
                        }
                    }).ToList());
            }
        }

        internal void CenterRowContent(List<int> columnLengths)
        {
            for (int i = 0; i < FormattedRows.Count; i++)
            {
                for (int j = 0; j < FormattedRows[i].Count; j++)
                {
                    if (TextAligmentData.ContainsKey(j) && TextAligmentData[j] == TextAligntment.Center)
                    {
                        FormattedRows[i][j] = CenteredString(FormattedRows[i][j], columnLengths[j]);
                    }
                }
            }
        }

        internal string[] CenterColumnContent(string[] columnSlices, List<int> columnLengths)
        {
            for (int i = 0; i < columnSlices.Length; i++)
            {
                if (HeaderTextAligmentData.ContainsKey(i))
                {
                    if (HeaderTextAligmentData[i] == TextAligntment.Center)
                    {
                        columnSlices[i] = CenteredString(columnSlices[i], columnLengths[i]);
                    }
                }
                else
                {
                    if (TextAligmentData.ContainsKey(i) && TextAligmentData[i] == TextAligntment.Center)
                    {
                        columnSlices[i] = CenteredString(columnSlices[i], columnLengths[i]);
                    }
                }
            }

            return columnSlices;
        }

        private string CenteredString(object s, int width)
        {
            if (s == null)
            {
                return null;
            }

            if (s.ToString().Length >= width)
            {
                return s.ToString();
            }

            int leftPadding = (width - s.ToString().Length) / 2;
            int rightPadding = width - s.ToString().Length - leftPadding;

            return new string(' ', leftPadding) + s + new string(' ', rightPadding);
        }

        internal List<int> GetCadidateColumnLengths(bool withUtf8Characters = true)
        {
            var columnLengths = new List<int>();

            var numberOfColumns = 0;
            if (FormattedRows.Any())
            {
                numberOfColumns = FormattedRows.Max(x => x.Count);
            }
            else
            {
                if (FormattedColumns != null)
                {
                    numberOfColumns = FormattedColumns.Count;
                }
            }

            if (numberOfColumns == 0)
            {
                return new List<int>();
            }

            if (numberOfColumns < FormattedColumns.Count)
            {
                numberOfColumns = FormattedColumns.Count;
            }

            for (var i = 0; i < numberOfColumns; i++)
            {
                var maxRow = 0;
                if (FormattedRows.Any())
                {
                    maxRow = FormattedRows
                        .Where(x => i < x.Count)
                        .Select(x => x[i]) // list cells of column i
                        .Max(x => x == null ? 0 : x.ToString().RealLength(withUtf8Characters));
                }

                if (FormattedColumns.ToArray().Length > i && (FormattedColumns[i] ?? string.Empty).ToString().RealLength(withUtf8Characters) > maxRow)
                {
                    maxRow = FormattedColumns[i].ToString().RealLength(withUtf8Characters);
                }

                if (MinLengthData != null && MinLengthData.ContainsKey(i))
                {
                    columnLengths.Add(maxRow > MinLengthData[i] ? maxRow : MinLengthData[i]);
                }
                else
                {
                    columnLengths.Add(maxRow);
                }
            }

            //if (!columnLengths.Any())
            //{
            //    throw new Exception("Table has no columns");
            //}

            if (this.CanTrimColumn)
            {
                if (columnLengths.Any())
                {
                    var temp = columnLengths;

                    //for (int i = 0; i < temp.Count; i++)
                    //{
                    //    if (temp[i] == 0)
                    //    {
                    //        columnLengths.RemoveAt(0);
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}

                    for (int i = temp.Count - 1; i >= 0; i--)
                    {
                        if (temp[i] == 0)
                        {
                            columnLengths.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return columnLengths;
        }

        public int NumberOfColumns { get { return this.GetCadidateColumnLengths().Count; } }
        public int NumberOfRows { get { return this.Rows.Count; } }

        //internal string Format(char delimiter)
        //{
        //    string delim = delimiter == '\0' ? string.Empty : delimiter.ToString();

        //    var columnLengths = GetCadidateColumnLengths();

        //    // | {0,-14} | {1,-29} | {2,-13} | {3,-3} | {4,-22} |
        //    if (columnLengths.Count > 0)
        //    {
        //        var format = Enumerable.Range(0, columnLengths.Count)
        //                    .Select(i => PaddingLeft + "{" + i + "," + (TextAligmentData == null ? "-" : (TextAligmentData.ContainsKey(i) ? TextAligmentData[i].ToString() : "-")) + columnLengths[i] + "}" + PaddingRight)
        //                    .Aggregate((s, a) => s + delim + a);

        //        return delim + format + delim;
        //    }
        //    else
        //    {
        //        return string.Empty;
        //    }
        //}

        private string EmbedTitle(string line)
        {
            var originalTitleLength = TableTitle.Length;

            if (!string.IsNullOrEmpty(TableTitle) && TableTitle.Trim().Length > 0) // !IsNullOrWhiteSpace
            {
                if (TableTitle.Length > line.Length - 4)
                {
                    TableTitle = TableTitle.Substring(0, line.Length - 4);

                    if (originalTitleLength != TableTitle.Length && TableTitle.Length > 3)
                    {
                        TableTitle = TableTitle.Substring(0, TableTitle.Length - 3) + "...";
                    }
                }

                TableTitle = TableTitle.Trim();
                TableTitle = " " + TableTitle + " ";
                var startPoint = 0;
                switch (TableTitleTextAlignment)
                {
                    case TextAligntment.Left:
                        startPoint = 1;
                        break;
                    case TextAligntment.Right:
                        startPoint = line.Length - 1 - TableTitle.RealLength(true);
                        break;
                    case TextAligntment.Center:
                        startPoint = (line.Length - TableTitle.RealLength(true)) / 2;
                        break;
                    default:
                        break;
                }

                TitlePositionStartAt = startPoint;
                var newBeginTableFormat = line.Substring(0, startPoint);
                newBeginTableFormat += TableTitle;
                TitlePositionLength = TableTitle.Length;
                int reallength = newBeginTableFormat.RealLength(true);
                newBeginTableFormat += line.Substring(reallength, line.Length - reallength);

                line = newBeginTableFormat;
                line = line.Replace("\0", " ");
            }

            return line;
        }
        #region Table lines

        internal string CreateTableTopLine(List<int> columnLengths, Dictionary<CharMapPositions, char> definition)
        {
            var borderTop = definition[CharMapPositions.BorderTop];
            var topLeft = definition[CharMapPositions.TopLeft];
            var topCenter = definition[CharMapPositions.TopCenter];
            var topRight = definition[CharMapPositions.TopRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i => new string(borderTop, columnLengths[i] + (PaddingLeft + PaddingRight).Length))
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : topCenter.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : topLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : topRight.ToString());

                line = EmbedTitle(line);

                if (line.Trim('\0').Length == 0)
                {
                    line = string.Empty;
                }

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        internal string CreateTableContentLineFormat(List<int> columnLengths, Dictionary<CharMapPositions, char> definition)
        {
            var borderLeft = definition[CharMapPositions.BorderLeft];
            var divider = definition[CharMapPositions.DividerY];
            var borderRight = definition[CharMapPositions.BorderRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i =>
                            {
                                var alignmentChar = string.Empty;
                                if (TextAligmentData == null || !TextAligmentData.ContainsKey(i) || TextAligmentData[i] == TextAligntment.Left)
                                {
                                    alignmentChar = "-";
                                }
                                return PaddingLeft + "{" + i + "," + alignmentChar + columnLengths[i] + "}" + PaddingRight;
                            })
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : divider.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : borderLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : borderRight.ToString());

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        internal string CreateRawLineFormat(List<int> columnLengths, Dictionary<CharMapPositions, char> definition, params object[] args)
        {
            var borderLeft = definition[CharMapPositions.BorderLeft];
            var divider = definition[CharMapPositions.DividerY];
            var borderRight = definition[CharMapPositions.BorderRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i =>
                            {
                                var alignmentChar = string.Empty;
                                if (TextAligmentData == null || !TextAligmentData.ContainsKey(i) || TextAligmentData[i] == TextAligntment.Left)
                                {
                                    alignmentChar = "-";
                                }
                                if (args.Length > i)
                                {
                                    string value = args[i]?.ToString() ?? "";
                                    return PaddingLeft + "{" + i + "," + alignmentChar + (columnLengths[i] - (value.RealLength(true) - value.Length)) + "}" + PaddingRight;
                                }
                                else
                                    return PaddingLeft + "{" + i + "," + alignmentChar + columnLengths[i] + "}" + PaddingRight;

                            })
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : divider.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : borderLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : borderRight.ToString());

                return line;
            }
            else
            {
                return string.Empty;
            }
        }
        internal string CreateTableMiddleLine(List<int> columnLengths, Dictionary<CharMapPositions, char> definition)
        {
            var dividerX = definition[CharMapPositions.DividerX];
            var middleLeft = definition[CharMapPositions.MiddleLeft];
            var middleCenter = definition[CharMapPositions.MiddleCenter];
            var middleRight = definition[CharMapPositions.MiddleRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i => new string(dividerX, columnLengths[i] + (PaddingLeft + PaddingRight).Length))
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : middleCenter.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : middleLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : middleRight.ToString());

                if (line.Trim('\0').Length == 0)
                {
                    line = string.Empty;
                }

                return line;
            }
            else
            {
                return string.Empty;
            }
        }


        internal string CreateTableBottomLine(List<int> columnLengths, Dictionary<CharMapPositions, char> definition)
        {
            var borderBottom = definition[CharMapPositions.BorderBottom];
            var bottomLeft = definition[CharMapPositions.BottomLeft];
            var bottomCenter = definition[CharMapPositions.BottomCenter];
            var bottomRight = definition[CharMapPositions.BottomRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i => new string(borderBottom, columnLengths[i] + (PaddingLeft + PaddingRight).Length))
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : bottomCenter.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : bottomLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : bottomRight.ToString());

                if (line.Trim('\0').Length == 0)
                {
                    line = string.Empty;
                }

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion


        #region Header lines

        internal string CreateHeaderTopLine(List<int> columnLengths, Dictionary<CharMapPositions, char> definition, Dictionary<HeaderCharMapPositions, char> headerDefinition)
        {
            var borderTop = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BorderTop) ? headerDefinition[HeaderCharMapPositions.BorderTop] : definition[CharMapPositions.BorderTop];
            var topLeft = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.TopLeft) ? headerDefinition[HeaderCharMapPositions.TopLeft] : definition[CharMapPositions.TopLeft];
            var topCenter = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.TopCenter) ? headerDefinition[HeaderCharMapPositions.TopCenter] : definition[CharMapPositions.TopCenter];
            var topRight = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.TopRight) ? headerDefinition[HeaderCharMapPositions.TopRight] : definition[CharMapPositions.TopRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i => new string(borderTop, columnLengths[i] + (PaddingLeft + PaddingRight).Length))
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : topCenter.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : topLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : topRight.ToString());

                line = EmbedTitle(line);

                if (line.Trim('\0').Length == 0)
                {
                    line = string.Empty;
                }

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        internal string CreateHeaderContentLineFormat(List<int> columnLengths, Dictionary<CharMapPositions, char> definition, Dictionary<HeaderCharMapPositions, char> headerDefinition)
        {
            var borderLeft = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BorderLeft) ? headerDefinition[HeaderCharMapPositions.BorderLeft] : definition[CharMapPositions.BorderLeft];
            var divider = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.Divider) ? headerDefinition[HeaderCharMapPositions.Divider] : definition[CharMapPositions.DividerY];
            var borderRight = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BorderRight) ? headerDefinition[HeaderCharMapPositions.BorderRight] : definition[CharMapPositions.BorderRight];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i =>
                            {
                                var alignmentChar = string.Empty;

                                if (HeaderTextAligmentData.ContainsKey(i))
                                {
                                    if (HeaderTextAligmentData[i] == TextAligntment.Left)
                                    {
                                        alignmentChar = "-";
                                    }
                                }
                                else
                                {
                                    if (TextAligmentData == null || !TextAligmentData.ContainsKey(i) || TextAligmentData[i] == TextAligntment.Left)
                                    {
                                        alignmentChar = "-";
                                    }
                                }

                                return PaddingLeft + "{" + i + "," + alignmentChar + columnLengths[i] + "}" + PaddingRight;
                            })
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : divider.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : borderLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : borderRight.ToString());

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        internal string CreateHeaderBottomLine(List<int> columnLengths, Dictionary<CharMapPositions, char> definition, Dictionary<HeaderCharMapPositions, char> headerDefinition)
        {
            var borderBottom = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BorderBottom) ? headerDefinition[HeaderCharMapPositions.BorderBottom] : definition[CharMapPositions.DividerX];
            var bottomLeft = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BottomLeft) ? headerDefinition[HeaderCharMapPositions.BottomLeft] : definition[CharMapPositions.MiddleLeft];
            var bottomCenter = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BottomCenter) ? headerDefinition[HeaderCharMapPositions.BottomCenter] : definition[CharMapPositions.MiddleRight];
            var bottomRight = headerDefinition != null && headerDefinition.ContainsKey(HeaderCharMapPositions.BottomRight) ? headerDefinition[HeaderCharMapPositions.BottomRight] : definition[CharMapPositions.MiddleCenter];

            if (columnLengths.Count > 0)
            {
                var result = Enumerable.Range(0, columnLengths.Count)
                            .Select(i => new string(borderBottom, columnLengths[i] + (PaddingLeft + PaddingRight).Length))
                            .Aggregate((s, a) => s + (CanRemoveDividerY() ? string.Empty : bottomCenter.ToString()) + a);

                var line = (CanRemoveBorderLeft() ? string.Empty : bottomLeft.ToString()) + result + (CanRemoveBorderRight() ? string.Empty : bottomRight.ToString());

                if (line.Trim('\0').Length == 0)
                {
                    line = string.Empty;
                }

                return line;
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion

        internal bool CanRemoveBorderLeft()
        {
            if (HeaderCharMapPositionStore == null)
            {
                return new List<char> {
                    CharMapPositionStore[CharMapPositions.TopLeft],
                    CharMapPositionStore[CharMapPositions.MiddleLeft],
                    CharMapPositionStore[CharMapPositions.BottomLeft],
                    CharMapPositionStore[CharMapPositions.BorderLeft]
                }
                .Select(x => x.ToString())
                .Aggregate((s, a) => s + a)
                .Replace("\0", string.Empty)
                .Trim().Length == 0;
            }
            else
            {
                var data = new List<char> { };
                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.TopLeft) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.TopLeft] : CharMapPositionStore[CharMapPositions.TopLeft]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.BorderLeft) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.BorderLeft] : CharMapPositionStore[CharMapPositions.BorderLeft]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.BottomLeft) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.BottomLeft] : CharMapPositionStore[CharMapPositions.MiddleLeft]);

                data.Add(CharMapPositionStore[CharMapPositions.MiddleLeft]);
                data.Add(CharMapPositionStore[CharMapPositions.BorderLeft]);
                data.Add(CharMapPositionStore[CharMapPositions.BottomLeft]);

                return
                    data
                        .Select(x => x.ToString())
                        .Aggregate((s, a) => s + a)
                        .Replace("\0", string.Empty)
                        .Trim().Length == 0;
            }
        }

        internal bool CanRemoveBorderRight()
        {
            if (HeaderCharMapPositionStore == null)
            {
                return new List<char> {
                    CharMapPositionStore[CharMapPositions.TopRight],
                    CharMapPositionStore[CharMapPositions.MiddleRight],
                    CharMapPositionStore[CharMapPositions.BottomRight],
                    CharMapPositionStore[CharMapPositions.BorderRight]
                }
                .Select(x => x.ToString())
                .Aggregate((s, a) => s + a)
                .Replace("\0", string.Empty)
                .Trim().Length == 0;
            }
            else
            {
                var data = new List<char> { };
                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.TopRight) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.TopRight] : CharMapPositionStore[CharMapPositions.TopRight]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.BorderRight) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.BorderRight] : CharMapPositionStore[CharMapPositions.BorderRight]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.BottomRight) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.BottomRight] : CharMapPositionStore[CharMapPositions.MiddleRight]);

                data.Add(CharMapPositionStore[CharMapPositions.MiddleRight]);
                data.Add(CharMapPositionStore[CharMapPositions.BorderRight]);
                data.Add(CharMapPositionStore[CharMapPositions.BottomRight]);

                return
                    data
                        .Select(x => x.ToString())
                        .Aggregate((s, a) => s + a)
                        .Replace("\0", string.Empty)
                        .Trim().Length == 0;
            }
        }

        internal bool CanRemoveDividerY()
        {
            if (HeaderCharMapPositionStore == null)
            {
                return new List<char> {
                    CharMapPositionStore[CharMapPositions.TopCenter],
                    CharMapPositionStore[CharMapPositions.MiddleCenter],
                    CharMapPositionStore[CharMapPositions.BottomCenter],
                    CharMapPositionStore[CharMapPositions.DividerY]
                }
                .Select(x => x.ToString())
                .Aggregate((s, a) => s + a)
                .Replace("\0", string.Empty)
                .Trim().Length == 0;
            }
            else
            {
                var data = new List<char> { };
                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.TopCenter) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.TopCenter] : CharMapPositionStore[CharMapPositions.TopCenter]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.Divider) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.Divider] : CharMapPositionStore[CharMapPositions.DividerY]);

                data.Add(HeaderCharMapPositionStore.ContainsKey(HeaderCharMapPositions.BottomCenter) ?
                    HeaderCharMapPositionStore[HeaderCharMapPositions.BottomCenter] : CharMapPositionStore[CharMapPositions.MiddleCenter]);

                data.Add(CharMapPositionStore[CharMapPositions.MiddleCenter]);
                data.Add(CharMapPositionStore[CharMapPositions.DividerY]);
                data.Add(CharMapPositionStore[CharMapPositions.BottomCenter]);

                return
                    data
                        .Select(x => x.ToString())
                        .Aggregate((s, a) => s + a)
                        .Replace("\0", string.Empty)
                        .Trim().Length == 0;
            }
        }
    }
}
