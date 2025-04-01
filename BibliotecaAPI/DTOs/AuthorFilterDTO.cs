namespace BibliotecaAPI.DTOs
{
    public class AuthorFilterDTO
    {
        public int Page { get; set; } = 1;
        public int RecordsPerPage { get; set; } = 10;
        public PaginationDTO PaginationDTO
        {
            get
            {
                return new PaginationDTO(Page, RecordsPerPage);
            }
        }
        public string? FirstNames { get; set; }
        public string? LastNames { get; set; }
        public bool? HasPhoto { get; set; }
        public bool? HasBooks { get; set; }
        public string? BookTitle { get; set; }
        public bool IncludeBooks { get; set; }
        public string? SortField { get; set; }
        public bool IsAscending { get; set; } = true;
    }
}
