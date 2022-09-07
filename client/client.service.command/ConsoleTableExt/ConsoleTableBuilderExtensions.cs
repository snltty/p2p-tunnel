using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ConsoleTableExt
{
    public static class ConsoleTableBuilderExtensions
    {
        public static ConsoleTableBuilder AddColumn(this ConsoleTableBuilder builder, string columnName)
        {
            builder.Column.Add(columnName);
            return builder;
        }

        public static ConsoleTableBuilder AddColumn(this ConsoleTableBuilder builder, List<string> columnNames)
        {
#if NET35
            columnNames.ForEach(f => builder.Column.Add(f));
#else
            builder.Column.AddRange(columnNames);
#endif
            return builder;
        }

        public static ConsoleTableBuilder AddColumn(this ConsoleTableBuilder builder, params string[] columnNames)
        {
            builder.Column.AddRange(new List<object>(columnNames));
            return builder;
        }

        public static ConsoleTableBuilder WithColumn(this ConsoleTableBuilder builder, List<string> columnNames)
        {
            builder.Column = new List<object>();
#if NET35
            columnNames.ForEach(f => builder.Column.Add(f));
#else
            builder.Column.AddRange(columnNames);
#endif
            return builder;
        }

        public static ConsoleTableBuilder WithColumn(this ConsoleTableBuilder builder, params string[] columnNames)
        {
            builder.Column = new List<object>();
            builder.Column.AddRange(new List<object>(columnNames));
            return builder;
        }

        public static ConsoleTableBuilder AddRow(this ConsoleTableBuilder builder, params object[] rowValues)
        {
            if (rowValues == null)
                return builder;

            builder.Rows.Add(new List<object>(rowValues));

            return builder;
        }

        public static ConsoleTableBuilder WithMetadataRow(this ConsoleTableBuilder builder, MetaRowPositions position, Func<ConsoleTableBuilder, string> contentGenerator)
        {
            switch (position)
            {
                case MetaRowPositions.Top:
                    if (builder.TopMetadataRows == null)
                    {
                        builder.TopMetadataRows = new List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>>();
                    }

                    builder.TopMetadataRows.Add(new KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>(position, contentGenerator));
                    break;
                case MetaRowPositions.Bottom:
                    if (builder.BottomMetadataRows == null)
                    {
                        builder.BottomMetadataRows = new List<KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>>();
                    }

                    builder.BottomMetadataRows.Add(new KeyValuePair<MetaRowPositions, Func<ConsoleTableBuilder, string>>(position, contentGenerator));
                    break;

                default:
                    break;
            }

            return builder;
        }

        /// <summary>
        /// Add title row on top of table
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static ConsoleTableBuilder WithTitle(this ConsoleTableBuilder builder, string title, TextAligntment titleAligntment = TextAligntment.Center)
        {
            builder.TableTitle = title;
            builder.TableTitleTextAlignment = titleAligntment;
            return builder;
        }

        /// <summary>
        /// Add title row on top of table
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="foregroundColor">text color</param>
        /// <returns></returns>
        public static ConsoleTableBuilder WithTitle(this ConsoleTableBuilder builder, string title, ConsoleColor foregroundColor, TextAligntment titleAligntment = TextAligntment.Center)
        {
            builder.TableTitle = title;
            builder.TableTitleColor = new ConsoleColorNullable(foregroundColor);
            builder.TableTitleTextAlignment = titleAligntment;
            return builder;
        }

        /// <summary>
        /// Add title row on top of table
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="foregroundColor">text color</param>
        /// <param name="backgroundColor">background color</param>
        /// <returns></returns>
        public static ConsoleTableBuilder WithTitle(this ConsoleTableBuilder builder, string title, ConsoleColor foregroundColor, ConsoleColor backgroundColor, TextAligntment titleAligntment = TextAligntment.Center)
        {
            builder.TableTitle = title;
            builder.TableTitleColor = new ConsoleColorNullable(foregroundColor, backgroundColor);
            builder.TableTitleTextAlignment = titleAligntment;
            return builder;
        }

        public static ConsoleTableBuilder WithPaddingLeft(this ConsoleTableBuilder builder, string paddingLeft)
        {
            builder.PaddingLeft = paddingLeft ?? string.Empty;
            return builder;
        }

        public static ConsoleTableBuilder WithPaddingRight(this ConsoleTableBuilder builder, string paddingRight)
        {
            builder.PaddingRight = paddingRight ?? string.Empty;
            return builder;
        }

  
        public static ConsoleTableBuilder WithFormatter(this ConsoleTableBuilder builder, int columnIndex, Func<object, string> formatter)
        {
            if (!builder.FormatterStore.ContainsKey(columnIndex))
            {
                builder.FormatterStore.Add(columnIndex, formatter);
            }
            else
            {
                builder.FormatterStore[columnIndex] = formatter;
            }

            return builder;
        }
        public static ConsoleTableBuilder WithColumnFormatter(this ConsoleTableBuilder builder, int columnIndex, Func<string, string> formatter)
        {
            if (!builder.ColumnFormatterStore.ContainsKey(columnIndex))
            {
                builder.ColumnFormatterStore.Add(columnIndex, formatter);
            }
            else
            {
                builder.ColumnFormatterStore[columnIndex] = formatter;
            }

            return builder;
        }

        /// <summary>
        /// Text alignment definition
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="alignmentData"></param>
        /// <returns></returns>
        public static ConsoleTableBuilder WithTextAlignment(this ConsoleTableBuilder builder, Dictionary<int, TextAligntment> alignmentData)
        {
            if (alignmentData != null)
            {
                builder.TextAligmentData = alignmentData;
            }

            return builder;
        }

        public static ConsoleTableBuilder WithHeaderTextAlignment(this ConsoleTableBuilder builder, Dictionary<int, TextAligntment> alignmentData)
        {
            if (alignmentData != null)
            {
                builder.HeaderTextAligmentData = alignmentData;
            }

            return builder;
        }

        public static ConsoleTableBuilder WithMinLength(this ConsoleTableBuilder builder, Dictionary<int, int> minLengthData)
        {
            if (minLengthData != null)
            {
                builder.MinLengthData = minLengthData;
            }

            return builder;
        }

        public static ConsoleTableBuilder TrimColumn(this ConsoleTableBuilder builder, bool canTrimColumn = true)
        {
            builder.CanTrimColumn = canTrimColumn;
            return builder;
        }

        public static ConsoleTableBuilder AddRow(this ConsoleTableBuilder builder, List<object> row)
        {
            if (row == null)
                return builder;

            builder.Rows.Add(row);
            return builder;
        }

        public static ConsoleTableBuilder AddRow(this ConsoleTableBuilder builder, List<List<object>> rows)
        {
            if (rows == null)
                return builder;

            builder.Rows.AddRange(rows);
            return builder;
        }

        public static ConsoleTableBuilder AddRow(this ConsoleTableBuilder builder, DataRow row)
        {
            if (row == null)
                return builder;

            builder.Rows.Add(new List<object>(row.ItemArray));
            return builder;
        }

        public static ConsoleTableBuilder WithFormat(this ConsoleTableBuilder builder, ConsoleTableBuilderFormat format)
        {
            // reset CharMapPositions
            builder.CharMapPositionStore = null;
            builder.TableFormat = format;

            switch (builder.TableFormat)
            {
                case ConsoleTableBuilderFormat.Default:
                    builder.CharMapPositionStore = new Dictionary<CharMapPositions, char>
                    {
                        { CharMapPositions.TopLeft, '-' },
                        { CharMapPositions.TopCenter, '-' },
                        { CharMapPositions.TopRight, '-' },
                        { CharMapPositions.MiddleLeft, '-' },
                        { CharMapPositions.MiddleCenter, '-' },
                        { CharMapPositions.MiddleRight, '-' },
                        { CharMapPositions.BottomLeft, '-' },
                        { CharMapPositions.BottomCenter, '-' },
                        { CharMapPositions.BottomRight, '-' },
                        { CharMapPositions.BorderTop, '-' },
                        { CharMapPositions.BorderLeft, '|' },
                        { CharMapPositions.BorderRight, '|' },
                        { CharMapPositions.BorderBottom, '-' },
                        { CharMapPositions.DividerX, '-' },
                        { CharMapPositions.DividerY, '|' },
                    };
                    break;
                case ConsoleTableBuilderFormat.MarkDown:
                    builder.CharMapPositionStore = new Dictionary<CharMapPositions, char>
                    {
                        { CharMapPositions.DividerY, '|' },
                        { CharMapPositions.BorderLeft, '|' },
                        { CharMapPositions.BorderRight, '|' },
                    };

                    builder.HeaderCharMapPositionStore = new Dictionary<HeaderCharMapPositions, char>
                    {
                        { HeaderCharMapPositions.BorderBottom, '-' },
                        { HeaderCharMapPositions.BottomLeft, '|' },
                        { HeaderCharMapPositions.BottomCenter, '|' },
                        { HeaderCharMapPositions.BottomRight, '|' },
                        { HeaderCharMapPositions.BorderLeft, '|' },
                        { HeaderCharMapPositions.BorderRight, '|' },
                        { HeaderCharMapPositions.Divider, '|' },
                    };
                    break;
                case ConsoleTableBuilderFormat.Alternative:
                    builder.CharMapPositionStore = new Dictionary<CharMapPositions, char>
                    {
                        { CharMapPositions.TopLeft, '+' },
                        { CharMapPositions.TopCenter, '+' },
                        { CharMapPositions.TopRight, '+' },
                        { CharMapPositions.MiddleLeft, '+' },
                        { CharMapPositions.MiddleCenter, '+' },
                        { CharMapPositions.MiddleRight, '+' },
                        { CharMapPositions.BottomLeft, '+' },
                        { CharMapPositions.BottomCenter, '+' },
                        { CharMapPositions.BottomRight, '+' },
                        { CharMapPositions.BorderTop, '-' },
                        { CharMapPositions.BorderRight, '|' },
                        { CharMapPositions.BorderBottom, '-' },
                        { CharMapPositions.BorderLeft, '|' },
                        { CharMapPositions.DividerX, '-' },
                        { CharMapPositions.DividerY, '|' },
                    };
                    break;
                case ConsoleTableBuilderFormat.Minimal:
                    builder.CharMapPositionStore = new Dictionary<CharMapPositions, char> { };

                    builder.HeaderCharMapPositionStore = new Dictionary<HeaderCharMapPositions, char>
                    {
                        { HeaderCharMapPositions.BorderBottom, '-' }
                    };

                    builder.PaddingLeft = string.Empty;
                    builder.PaddingRight = " ";
                    break;
                default:
                    break;
            }

            return builder;
        }

        public static ConsoleTableBuilder WithCharMapDefinition(this ConsoleTableBuilder builder)
        {
            return builder.WithCharMapDefinition(new Dictionary<CharMapPositions, char> { });
        }

        public static ConsoleTableBuilder WithCharMapDefinition(this ConsoleTableBuilder builder, Dictionary<CharMapPositions, char> charMapPositions)
        {
            builder.CharMapPositionStore = charMapPositions;
            return builder;
        }

        public static ConsoleTableBuilder WithCharMapDefinition(this ConsoleTableBuilder builder, Dictionary<CharMapPositions, char> charMapPositions, Dictionary<HeaderCharMapPositions, char> headerCharMapPositions = null)
        {
            builder.CharMapPositionStore = charMapPositions;
            builder.HeaderCharMapPositionStore = headerCharMapPositions;
            return builder;
        }

        public static ConsoleTableBuilder WithHeaderCharMapDefinition(this ConsoleTableBuilder builder, Dictionary<HeaderCharMapPositions, char> headerCharMapPositions = null)
        {
            builder.HeaderCharMapPositionStore = headerCharMapPositions;
            return builder;
        }

        public static StringBuilder Export(this ConsoleTableBuilder builder)
        {
            var numberOfColumns = 0;
            if (builder.Rows.Any())
            {
                numberOfColumns = builder.Rows.Max(x => x.Count);
            }
            else
            {
                if (builder.Column != null)
                {
                    numberOfColumns = builder.Column.Count();
                }
            }

            if (numberOfColumns == 0)
            {
                return new StringBuilder();
            }

            if (builder.Column == null)
            {
                numberOfColumns = 0;
            }
            else
            {
                if (numberOfColumns < builder.Column.Count)
                {
                    numberOfColumns = builder.Column.Count;
                }
            }

            for (int i = 0; i < 1; i++)
            {
                if (builder.Column != null && builder.Column.Count < numberOfColumns)
                {
                    var missCount = numberOfColumns - builder.Column.Count;
                    for (int j = 0; j < missCount; j++)
                    {
                        builder.Column.Add(null);
                    }
                }
            }

            for (int i = 0; i < builder.Rows.Count; i++)
            {
                if (builder.Rows[i].Count < numberOfColumns)
                {
                    var missCount = numberOfColumns - builder.Rows[i].Count;
                    for (int j = 0; j < missCount; j++)
                    {
                        builder.Rows[i].Add(null);
                    }
                }
            }

            return CreateTableForCustomFormat(builder);
        }

        public static void ExportAndWrite(this ConsoleTableBuilder builder, TableAligntment alignment = TableAligntment.Left)
        {
            var strBuilder = builder.Export();
            var lines = strBuilder.ToString().Split('\n');

            var linesCount = lines.Count();
            for (int i = 0; i < linesCount; i++)
            {
                var row = string.Empty;

                switch (alignment)
                {
                    case TableAligntment.Left:
                        row = lines[i];
                        break;
                    case TableAligntment.Center:
                        row = String.Format("{0," + ((Console.WindowWidth / 2) + (lines[i].RealLength(true) / 2) - (lines[i].RealLength(true) - lines[i].Length)) + "}", lines[i]);
                        break;
                    case TableAligntment.Right:
                        var count = Console.WindowWidth - lines[i].RealLength(true);
                        row = new string(' ', count<0?0: count) + lines[i];
                        break;
                }

                if (i == 0
                    && !string.IsNullOrEmpty(builder.TableTitle)
                    && builder.TableTitle.Trim().Length != 0
                    && !builder.TableTitleColor.IsForegroundColorNull
                    && builder.TitlePositionStartAt > 0
                    && builder.TitlePositionLength > 0)
                {
                    var newTitlePositionStartAt = builder.TitlePositionStartAt + (row.Length - lines[i].Length);

                    Console.Write(row.Substring(0, newTitlePositionStartAt));
                    Console.ForegroundColor = builder.TableTitleColor.ForegroundColor;
                    if (!builder.TableTitleColor.IsBackgroundColorNull)
                    {
                        Console.BackgroundColor = builder.TableTitleColor.BackgroundColor;
                    }
                    Console.Write(row.Substring(newTitlePositionStartAt, builder.TitlePositionLength));
                    Console.ResetColor();
                    Console.Write(row.Substring(newTitlePositionStartAt + builder.TitlePositionLength, row.Length - (newTitlePositionStartAt + builder.TitlePositionLength)));
                    Console.Write('\n');
                }
                else
                {
                    if (i == linesCount - 2)
                    {
                        if (row.EndsWith('\r'.ToString()))
                        {
                            Console.Write(row.Substring(0, row.Length - 1));
                        }
                        else
                        {
                            Console.Write(row);
                        }
                    }
                    else
                    {
                        if (i == linesCount - 1) // is last line
                        {
                            Console.Write(row);
                        }
                        else
                        {
                            Console.WriteLine(row);
                        }
                    }
                }
            }
        }

        public static void ExportAndWriteLine(this ConsoleTableBuilder builder, TableAligntment alignment = TableAligntment.Left)
        {
            builder.ExportAndWrite(alignment);
            Console.Write('\n');
        }

        private static StringBuilder CreateTableForCustomFormat(ConsoleTableBuilder builder)
        {
            if (builder.CharMapPositionStore == null)
            {
                builder.WithFormat(ConsoleTableBuilderFormat.Default);
            }

            builder.PopulateFormattedColumnsRows();
            var columnLengths = builder.GetCadidateColumnLengths();
            var columnNoUtf8CharasLengths = builder.GetCadidateColumnLengths(false);
            builder.CenterRowContent(columnLengths);

            var filledMap = FillCharMap(builder.CharMapPositionStore);
            var filledHeaderMap = FillHeaderCharMap(builder.HeaderCharMapPositionStore);

            var strBuilder = new StringBuilder();
            var topMetadataStringBuilder = BuildMetaRowsFormat(builder, MetaRowPositions.Top);
            for (int i = 0; i < topMetadataStringBuilder.Count; i++)
            {
                strBuilder.AppendLine(topMetadataStringBuilder[i]);
            }

            var tableTopLine = builder.CreateTableTopLine(columnLengths, filledMap);
            var tableRowContentFormat = builder.CreateTableContentLineFormat(columnLengths, filledMap);
            var tableMiddleLine = builder.CreateTableMiddleLine(columnLengths, filledMap);
            var tableBottomLine = builder.CreateTableBottomLine(columnLengths, filledMap);

            var headerTopLine = string.Empty;
            var headerRowContentFormat = string.Empty;
            var headerBottomLine = string.Empty;

            if (filledHeaderMap != null)
            {
                headerTopLine = builder.CreateHeaderTopLine(columnLengths, filledMap, filledHeaderMap);
                headerRowContentFormat = builder.CreateHeaderContentLineFormat(columnLengths, filledMap, filledHeaderMap);
                headerBottomLine = builder.CreateHeaderBottomLine(columnLengths, filledMap, filledHeaderMap);
            }

            // find the longest formatted line
            //var maxRowLength = Math.Max(0, builder.Rows.Any() ? builder.Rows.Max(row => string.Format(tableRowContentFormat, row.ToArray()).Length) : 0);

            var hasHeader = builder.FormattedColumns != null && builder.FormattedColumns.Any() && builder.FormattedColumns.Max(x => (x ?? string.Empty).ToString().Length) > 0;

            // header
            if (hasHeader)
            {
                if (headerTopLine != null && headerTopLine.Trim().Length > 0)
                {
                    strBuilder.AppendLine(headerTopLine);
                }
                else
                {
                    if (tableTopLine != null && tableTopLine.Trim().Length > 0)
                    {
                        strBuilder.AppendLine(tableTopLine);
                    }
                }

                var headerSlices = builder.FormattedColumns.ToArray();
                var formattedHeaderSlice = builder.CenterColumnContent(headerSlices, columnLengths);

                //var formattedHeaderSlice = Enumerable.Range(0, headerSlices.Length).Select(idx => builder.ColumnFormatterStore.ContainsKey(idx) ? builder.ColumnFormatterStore[idx](headerSlices[idx] == null ? string.Empty : headerSlices[idx].ToString()) : headerSlices[idx] == null ? string.Empty : headerSlices[idx].ToString()).ToArray();
                //formattedHeaderSlice = builder.CenterColumnContent(headerSlices, columnLengths);

                if (headerRowContentFormat != null && headerRowContentFormat.Trim().Length > 0)
                {
                    strBuilder.AppendLine(string.Format(headerRowContentFormat, formattedHeaderSlice));
                }
                else
                {
                    strBuilder.AppendLine(string.Format(tableRowContentFormat, formattedHeaderSlice));
                }
            }
            //else
            //{
            //    if (beginTableFormat.Length > 0) strBuilder.AppendLine(beginTableFormat);
            //    strBuilder.AppendLine(string.Format(rowContentTableFormat, builder.FormattedColumns.ToArray()));
            //}

            // add each row

            //var results = builder.Rows.Select(row => {
            //    var rowSlices = row.ToArray();
            //    return string.Format(tableRowContentFormat, Enumerable.Range(0, rowSlices.Length).Select(idx => builder.FormatterStore.ContainsKey(idx) ? builder.FormatterStore[idx](rowSlices[idx] == null ? string.Empty : rowSlices[idx].ToString()) : rowSlices[idx] == null ? string.Empty : rowSlices[idx].ToString()).ToArray());
            //}).ToList();

            var results = builder.FormattedRows.Select(row =>
            {
                var rowFormate = builder.CreateRawLineFormat(columnLengths, filledMap, row.ToArray());
                return string.Format(rowFormate, row.ToArray());

            }).ToList();

            var isFirstRow = true;
            foreach (var row in results)
            {
                if (isFirstRow)
                {
                    if (hasHeader)
                    {
                        if ((string.IsNullOrEmpty(headerBottomLine) || headerBottomLine.Length == 0) && tableMiddleLine.Length > 0)
                        {
                            strBuilder.AppendLine(tableMiddleLine);
                        }
                        else
                        {
                            if (headerBottomLine.Length > 0)
                            {
                                strBuilder.AppendLine(headerBottomLine);
                            }
                        }
                    }
                    else
                    {
                        if (tableTopLine.Length > 0)
                        {
                            strBuilder.AppendLine(tableTopLine);
                        }
                    }

                    isFirstRow = false;
                }
                else
                {
                    if (tableMiddleLine.Length > 0)
                    {
                        strBuilder.AppendLine(tableMiddleLine);
                    }
                }

                strBuilder.AppendLine(row);
            }

            if (results.Any())
            {
                if (tableBottomLine.Length > 0)
                {
                    strBuilder.AppendLine(tableBottomLine);
                }
            }
            else
            {
                if ((string.IsNullOrEmpty(headerBottomLine) || headerBottomLine.Length == 0) && tableBottomLine.Length > 0)
                {
                    strBuilder.AppendLine(tableBottomLine);
                }
                else
                {
                    if (headerBottomLine.Length > 0)
                    {
                        strBuilder.AppendLine(headerBottomLine);
                    }
                }
            }

            var bottomMetadataStringBuilder = BuildMetaRowsFormat(builder, MetaRowPositions.Bottom);
            for (int i = 0; i < bottomMetadataStringBuilder.Count; i++)
            {
                strBuilder.AppendLine(bottomMetadataStringBuilder[i]);
            }

            return strBuilder;
        }

        //private static StringBuilder CreateTableForDefaultFormat(ConsoleTableBuilder builder)
        //{
        //    var strBuilder = new StringBuilder();
        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Top);

        //    // create the string format with padding
        //    var format = builder.Format('|');

        //    if (format == string.Empty)
        //    {
        //        return strBuilder;
        //    }

        //    // find the longest formatted line
        //    var maxRowLength = Math.Max(0, builder.Rows.Any() ? builder.Rows.Max(row => string.Format(format, row.ToArray()).Length) : 0);

        //    // add each row
        //    var results = builder.Rows.Select(row => string.Format(format, row.ToArray())).ToList();

        //    // create the divider
        //    var divider = new string('-', maxRowLength);

        //    // header
        //    if (builder.Column != null && builder.Column.Any() && builder.Column.Max(x => (x ?? string.Empty).ToString().Length) > 0)
        //    {
        //        strBuilder.AppendLine(divider);
        //        strBuilder.AppendLine(string.Format(format, builder.Column.ToArray()));
        //    }

        //    foreach (var row in results)
        //    {
        //        strBuilder.AppendLine(divider);
        //        strBuilder.AppendLine(row);
        //    }

        //    strBuilder.AppendLine(divider);

        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Bottom);
        //    return strBuilder;
        //}

        //private static StringBuilder CreateTableForMinimalFormat(ConsoleTableBuilder builder)
        //{
        //    var strBuilder = new StringBuilder();
        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Top);

        //    // create the string format with padding
        //    var format = builder.Format('\0').Trim();

        //    if (format == string.Empty)
        //    {
        //        return strBuilder;
        //    }

        //    var skipFirstRow = false;
        //    var columnHeaders = string.Empty;

        //    if (builder.Column != null && builder.Column.Any() && builder.Column.Max(x => (x ?? string.Empty).ToString().Length) > 0)
        //    {
        //        skipFirstRow = false;
        //        columnHeaders = string.Format(format, builder.Column.ToArray());
        //    }
        //    else
        //    {
        //        skipFirstRow = true;
        //        columnHeaders = string.Format(format, builder.Rows.First().ToArray());
        //    }

        //    // create the divider
        //    var divider = Regex.Replace(columnHeaders, @"[^|]", '-'.ToString());

        //    strBuilder.AppendLine(columnHeaders);
        //    strBuilder.AppendLine(divider);

        //    // add each row
        //    var results = builder.Rows.Skip(skipFirstRow ? 1 : 0).Select(row => string.Format(format, row.ToArray())).ToList();
        //    results.ForEach(row => strBuilder.AppendLine(row));

        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Bottom);

        //    return strBuilder;
        //}

        //private static StringBuilder CreateTableForMarkdownFormat(ConsoleTableBuilder builder)
        //{
        //    var strBuilder = new StringBuilder();
        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Top);

        //    // create the string format with padding
        //    var format = builder.Format('|');

        //    if (format == string.Empty)
        //    {
        //        return strBuilder;
        //    }

        //    var skipFirstRow = false;
        //    var columnHeaders = string.Empty;

        //    if (builder.Column != null && builder.Column.Any() && builder.Column.Max(x => (x ?? string.Empty).ToString().Length) > 0)
        //    {
        //        skipFirstRow = false;
        //        columnHeaders = string.Format(format, builder.Column.ToArray());
        //    }
        //    else
        //    {
        //        skipFirstRow = true;
        //        columnHeaders = string.Format(format, builder.Rows.First().ToArray());
        //    }

        //    // create the divider
        //    var divider = Regex.Replace(columnHeaders, @"[^|]", '-'.ToString());

        //    strBuilder.AppendLine(columnHeaders);
        //    strBuilder.AppendLine(divider);

        //    // add each row
        //    var results = builder.Rows.Skip(skipFirstRow ? 1 : 0).Select(row => string.Format(format, row.ToArray())).ToList();
        //    results.ForEach(row => strBuilder.AppendLine(row));

        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Bottom);

        //    return strBuilder;
        //}

        //private static StringBuilder CreateTableForAlternativeFormat(ConsoleTableBuilder builder)
        //{
        //    var strBuilder = new StringBuilder();
        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Top);

        //    // create the string format with padding
        //    var format = builder.Format('|');

        //    if (format == string.Empty)
        //    {
        //        return strBuilder;
        //    }

        //    var skipFirstRow = false;
        //    var columnHeaders = string.Empty;

        //    if (builder.Column != null && builder.Column.Any() && builder.Column.Max(x => (x ?? string.Empty).ToString().Length) > 0)
        //    {
        //        skipFirstRow = false;
        //        columnHeaders = string.Format(format, builder.Column.ToArray());
        //    }
        //    else
        //    {
        //        skipFirstRow = true;
        //        columnHeaders = string.Format(format, builder.Rows.First().ToArray());
        //    }

        //    // create the divider
        //    var divider = Regex.Replace(columnHeaders, @"[^|]", '-'.ToString());
        //    var dividerPlus = divider.Replace("|", "+");

        //    strBuilder.AppendLine(dividerPlus);
        //    strBuilder.AppendLine(columnHeaders);

        //    // add each row
        //    var results = builder.Rows.Skip(skipFirstRow ? 1 : 0).Select(row => string.Format(format, row.ToArray())).ToList();

        //    foreach (var row in results)
        //    {
        //        strBuilder.AppendLine(dividerPlus);
        //        strBuilder.AppendLine(row);
        //    }
        //    strBuilder.AppendLine(dividerPlus);

        //    BuildMetaRowsFormat(builder, strBuilder, MetaRowPositions.Bottom);
        //    return strBuilder;
        //}

        private static List<string> BuildMetaRowsFormat(ConsoleTableBuilder builder, MetaRowPositions position)
        {
            var result = new List<string>();
            switch (position)
            {
                case MetaRowPositions.Top:
                    if (builder.TopMetadataRows.Any())
                    {
                        foreach (var item in builder.TopMetadataRows)
                        {
                            if (item.Value != null)
                            {
                                result.Add(item.Value.Invoke(builder));
                            }
                        }
                    }
                    break;
                case MetaRowPositions.Bottom:
                    if (builder.BottomMetadataRows.Any())
                    {
                        foreach (var item in builder.BottomMetadataRows)
                        {
                            if (item.Value != null)
                            {
                                result.Add(item.Value.Invoke(builder));
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        private static Dictionary<CharMapPositions, char> FillCharMap(Dictionary<CharMapPositions, char> definition)
        {
            if (definition == null)
            {
                return new Dictionary<CharMapPositions, char>();
            }

            var filledMap = definition;

            foreach (CharMapPositions c in (CharMapPositions[])Enum.GetValues(typeof(CharMapPositions)))
            {
                if (!filledMap.ContainsKey(c))
                {
                    filledMap.Add(c, '\0');
                }
            }

            return filledMap;
        }

        private static Dictionary<HeaderCharMapPositions, char> FillHeaderCharMap(Dictionary<HeaderCharMapPositions, char> definition)
        {
            if (definition == null)
            {
                return null;
            }

            var filledMap = definition;

            foreach (HeaderCharMapPositions c in (HeaderCharMapPositions[])Enum.GetValues(typeof(HeaderCharMapPositions)))
            {
                if (!filledMap.ContainsKey(c))
                {
                    filledMap.Add(c, '\0');
                }
            }

            return filledMap;
        }

    }
}
