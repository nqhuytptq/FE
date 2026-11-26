using System.IO.Compression;
using System.Security;
using System.Text;

namespace DebtManagement.Application.Services;

public static class SimpleXlsxExporter
{
    public static byte[] BuildWorkbook(IEnumerable<(string SheetName, List<string[]> Rows)> sheets)
    {
        var sheetList = sheets.ToList();
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            WriteEntry(zip, "[Content_Types].xml", BuildContentTypes(sheetList.Count));
            WriteEntry(zip, "_rels/.rels", RootRels);
            WriteEntry(zip, "xl/workbook.xml", BuildWorkbookXml(sheetList));
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(sheetList.Count));
            WriteEntry(zip, "xl/styles.xml", StylesXml);

            for (int i = 0; i < sheetList.Count; i++)
                WriteEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", BuildSheetXml(sheetList[i].Rows));
        }
        return ms.ToArray();
    }

    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        sb.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        sb.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        sb.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        sb.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private const string RootRels =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string BuildWorkbookXml(List<(string SheetName, List<string[]> Rows)> sheets)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>""");
        for (int i = 0; i < sheets.Count; i++)
        {
            var safeName = SecurityElement.Escape(sheets[i].SheetName) ?? $"Sheet{i + 1}";
            sb.Append($"""<sheet name="{safeName}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        }
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string BuildWorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        sb.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private const string StylesXml =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FFEAF5EE"/></fill></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="1" borderId="0" xfId="0" applyFont="1" applyFill="1"/></cellXfs></styleSheet>""";

    private static string BuildSheetXml(List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        for (int r = 0; r < rows.Count; r++)
        {
            var rowNum = r + 1;
            var styleAttr = r <= 1 ? """ s="1" """ : " ";
            sb.Append($"""<row r="{rowNum}">""");
            for (int c = 0; c < rows[r].Length; c++)
            {
                var cellRef = $"{ColName(c)}{rowNum}";
                var text = SecurityElement.Escape(rows[r][c] ?? "") ?? "";
                sb.Append($"""<c r="{cellRef}" t="inlineStr"{styleAttr}><is><t>{text}</t></is></c>""");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string ColName(int index)
    {
        var dividend = index + 1;
        var name = "";
        while (dividend > 0)
        {
            var mod = (dividend - 1) % 26;
            name = (char)('A' + mod) + name;
            dividend = (dividend - mod) / 26;
        }
        return name;
    }
}