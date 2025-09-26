using Microsoft.Extensions.Logging;
using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace SqlToXmlConverter.Services;

public class XmlConverterService
{
    private readonly ILogger<XmlConverterService> _logger;

    public XmlConverterService(ILogger<XmlConverterService> logger)
    {
        _logger = logger;
    }

    public string ConvertToXml<T>(List<T> data, string rootElementName = "Data")
    {
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(List<T>), new XmlRootAttribute(rootElementName));
            
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            });

            xmlSerializer.Serialize(xmlWriter, data);
            var xmlContent = stringWriter.ToString();

            _logger.LogInformation("Successfully converted {Count} items to XML", data.Count);
            return xmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while converting data to XML");
            throw;
        }
    }

    public async Task SaveXmlToFileAsync(string xmlContent, string filePath)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, xmlContent);
            _logger.LogInformation("XML content saved to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving XML to file: {FilePath}", filePath);
            throw;
        }
    }

    public string ConvertDataTableToXml(DataTable dataTable, string rootElementName = "Data")
    {
        try
        {
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            });

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement(rootElementName);

            foreach (DataRow row in dataTable.Rows)
            {
                xmlWriter.WriteStartElement("Record");
                
                foreach (DataColumn column in dataTable.Columns)
                {
                    xmlWriter.WriteStartElement(column.ColumnName);
                    xmlWriter.WriteValue(row[column]?.ToString() ?? string.Empty);
                    xmlWriter.WriteEndElement();
                }
                
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();

            var xmlContent = stringWriter.ToString();
            _logger.LogInformation("Successfully converted DataTable with {RowCount} rows to XML", dataTable.Rows.Count);
            return xmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while converting DataTable to XML");
            throw;
        }
    }
}
