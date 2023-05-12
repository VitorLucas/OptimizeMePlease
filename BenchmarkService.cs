using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OptimizeMePlease
{
    [MemoryDiagnoser]
    public class BenchmarkService
    {
        public BenchmarkService()
        {
        }

        /// <summary>
        /// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
        /// from country Serbia aged 27, with the highest BooksCount
        /// and all his/her books (Book Name/Title and Publishment Year) published before 1900
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public List<AuthorDTO> GetAuthors()
        {
            using var dbContext = new AppDbContext();

            var test = dbContext.Authors.FirstOrDefault();
            var authors = dbContext.Authors
                                        .Include(x => x.User)
                                        .ThenInclude(x => x.UserRoles)
                                        .ThenInclude(x => x.Role)
                                        .Include(x => x.Books)
                                        .ThenInclude(x => x.Publisher)
                                        .ToList()
                                        .Select(x => new AuthorDTO
                                        {
                                            UserCreated = x.User.Created,
                                            UserEmailConfirmed = x.User.EmailConfirmed,
                                            UserFirstName = x.User.FirstName,
                                            UserLastActivity = x.User.LastActivity,
                                            UserLastName = x.User.LastName,
                                            UserEmail = x.User.Email,
                                            UserName = x.User.UserName,
                                            UserId = x.User.Id,
                                            RoleId = x.User.UserRoles.FirstOrDefault(y => y.UserId == x.UserId).RoleId,
                                            BooksCount = x.BooksCount,
                                            Books = x.Books.Select(y => new BookDto
                                            {
                                                Id = y.Id,
                                                Name = y.Name,
                                                Published = y.Published,
                                                ISBN = y.ISBN,
                                                PublisherName = y.Publisher.Name
                                            }),
                                            AuthorAge = x.Age,
                                            AuthorCountry = x.Country,
                                            AuthorNickName = x.NickName,
                                            Id = x.Id
                                        })
                                        .ToList()
                                        .Where(x => x.AuthorCountry == "Serbia" && x.AuthorAge == 27)
                                        .ToList();

            var orderedAuthors = authors.OrderByDescending(x => x.BooksCount).Take(2).ToList();

            List<AuthorDTO> finalAuthors = new List<AuthorDTO>();
            foreach (var author in orderedAuthors)
            {
                List<BookDto> books = new List<BookDto>();

                var allBooks = author.Books;

                foreach (var book in allBooks)
                {
                    if (book.Published.Year < 1900)
                    {
                        book.PublishedYear = book.Published.Year;
                        books.Add(book);
                    }
                }

                author.Books = books;
                finalAuthors.Add(author);
            }

            return finalAuthors;
        }

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized()
        {
            using var dbContext = new AppDbContext();
            var date = new DateTime(1900, 1, 1);

            var authorsFiltered = dbContext.Authors
                                        .Include(x => x.User)
                                        .Include(x => x.Books)
                                        .Where(x => x.Country == "Serbia" && x.Age == 27)
                                        .Select(x => new AuthorDTO
                                        {
                                            UserFirstName = x.User.FirstName,
                                            UserLastName = x.User.LastName,
                                            UserEmail = x.User.Email,
                                            UserName = x.User.UserName,
                                            BooksCount = x.BooksCount,
                                            Books = x.Books.Select(y => new BookDto
                                            {
                                                Name = y.Name,
                                                Published = y.Published,
                                            }),
                                            AuthorAge = x.Age,
                                            AuthorCountry = x.Country,
                                        })
                                        .OrderByDescending(x => x.BooksCount)
                                        .Take(2)
                                        .ToList();


            List<AuthorDTO> foundAuthors = new();

            Parallel.ForEach(authorsFiltered, x =>
            {
                x.Books = x.Books.Where(z => z.Published.Year < 1900).ToList();
                foundAuthors.Add(x);
            });

            return foundAuthors;
        }

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized_Struct1()
        {
            using var dbContext = new AppDbContext();

            return dbContext.Authors
                .Where(x => x.Country == "Serbia" && x.Age == 27 && x.Books.Any(y => y.Published.Year < 1900))
                .OrderByDescending(x => x.BooksCount)
                .Select(x => new AuthorDTO
                {
                    UserFirstName = x.User.FirstName,
                    UserLastName = x.User.LastName,
                    UserEmail = x.User.Email,
                    UserName = x.User.UserName,
                    Books = x.Books.Select(y => new BookDto
                    {
                        Name = y.Name,
                        PublishedYear = y.Published.Year
                    }),
                    AuthorAge = x.Age,
                    AuthorCountry = x.Country,
                })
                .Take(2)
                .ToList();
        }

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized_Compiled()
        {
            List<AuthorDTO> foundAuthors = new();

            using var context = new AppDbContext();
            var authorsFiltered = CompiledQuery(context).ToList();

            Parallel.ForEach(authorsFiltered, x =>
            {
                x.Books = x.Books.Where(z => z.Published.Year < 1900).ToList();
                foundAuthors.Add(x);
            });

            return foundAuthors;
        }

        private static readonly Func<AppDbContext, IEnumerable<AuthorDTO>> CompiledQuery =
            EF.CompileQuery((AppDbContext context) =>
                context.Authors
                .Where(x => x.Country == "Serbia" && x.Age == 27)
                .OrderByDescending(x => x.BooksCount)
                .Select(x => new AuthorDTO
                {
                    UserFirstName = x.User.FirstName,
                    UserLastName = x.User.LastName,
                    UserEmail = x.User.Email,
                    UserName = x.User.UserName,
                    Books = x.Books.Select(y => new BookDto
                    {
                        Name = y.Name,
                        PublishedYear = y.Published.Year
                    }),
                    AuthorAge = x.Age,
                    AuthorCountry = x.Country,
                })
                .Take(2)
            );
    }
}
