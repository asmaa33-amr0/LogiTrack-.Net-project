namespace LogiTrackWebV1._0.DTOs
{
    // Wrapping the status in a DTO makes model binding reliable. Binding a bare
    // [FromBody] string requires the client to send a raw JSON string literal
    // ("Delivered"), which is easy to get wrong; a DTO accepts a normal object.
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
