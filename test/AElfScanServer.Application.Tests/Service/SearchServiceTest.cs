using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Dtos;
using AElfScanServer.HttpApi.Service;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElfScanServer.Service
{
    public class SearchServiceTest : AElfScanServerApplicationTestBase
    {
        private readonly ISearchService _searchService;

        public SearchServiceTest(ITestOutputHelper output) : base(output)
        {
            _searchService = GetRequiredService<ISearchService>();
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnExpectedResults()
        {
            // Arrange
            var request = new SearchRequestDto
            {
               
                Keyword = "ELF",
                FilterType =  FilterTypes.AllFilter ,
                SearchType = SearchTypes.FuzzySearch
                
            };

            // Act
            var response = await _searchService.SearchAsync(request);

            // Assert
            response.Tokens[0].Symbol.ShouldBe("ELF");
        }
    }
}