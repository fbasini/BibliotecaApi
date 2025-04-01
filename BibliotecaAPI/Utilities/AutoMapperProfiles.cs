using AutoMapper;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;

namespace BibliotecaAPI.Utilities
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Author, AuthorDTO>()
                .ForMember(dto => dto.FullName, config => config.MapFrom(author => MapAuthorName(author)));

            CreateMap<Author, AuthorWithBooksDTO>()
                .ForMember(dto => dto.FullName, config => config.MapFrom(author => MapAuthorName(author)));

            CreateMap<CreateAuthorDTO, Author>();
            CreateMap<CreateAuthorWithPhotoDTO, Author>()
                .ForMember(ent => ent.Photo, config => config.Ignore());
            CreateMap<Author, AuthorPatchDTO>().ReverseMap();

            CreateMap<AuthorBook, BookDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.BookId))
                .ForMember(dto => dto.Title, config => config.MapFrom(ent => ent.Book!.Title));


            CreateMap<Book, BookDTO>();
            CreateMap<CreateBookDTO, Book>()
                .ForMember(ent => ent.Authors, config =>
                    config.MapFrom(dto => dto.AuthorsIds.Select(id => new AuthorBook { AuthorId = id })));

            CreateMap<Book, BookWithAuthorsDTO>();

            CreateMap<AuthorBook, AuthorDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.AuthorId))
                .ForMember(dto => dto.FullName, config => config.MapFrom(ent => MapAuthorName(ent.Author!)));

            CreateMap<CreateCommentDTO,  Comment>();
            CreateMap<Comment, CommentDTO>()
                .ForMember(dto => dto.UserEmail, config => config.MapFrom(ent => ent.User!.Email));
            CreateMap<CommentPatchDTO, Comment>().ReverseMap();

            CreateMap<User, UserDTO>();
        }

        private string MapAuthorName(Author author) => $"{author.FirstName} {author.LastName}";
    }
}
