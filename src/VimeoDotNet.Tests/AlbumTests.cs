﻿using System.Threading.Tasks;
using Shouldly;
using VimeoDotNet.Models;
using VimeoDotNet.Parameters;
using Xunit;

namespace VimeoDotNet.Tests
{
    public class AlbumTests : BaseTest
    {
        [Fact]
        public async Task GetAlbumsShouldCorrectlyWorkForMe()
        {
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums",
                ResponseJsonFile = "Album.albums.json"
            });
            var client = CreateAuthenticatedClient();
            var albums = await client.GetAlbumsAsync(UserId.Me);
            albums.Total.ShouldBe(1);
            albums.PerPage.ShouldBe(25);
            albums.Data.Count.ShouldBe(1);
            albums.Data[0].Name.ShouldBe("Unit Test Album");
            albums.Paging.Next.ShouldBeNull();
            albums.Paging.Previous.ShouldBeNull();
            albums.Paging.First.ShouldBe("/me/albums?page=1");
            albums.Paging.Last.ShouldBe("/me/albums?page=1");
        }

        [Fact]
        public async Task GetAlbumsShouldCorrectlyWorkForUserId()
        {
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/users/115220313/albums",
                ResponseJsonFile = "Album.albums-115220313.json"
            });
            var client = CreateAuthenticatedClient();
            var albums = await client.GetAlbumsAsync(VimeoSettings.PublicUserId);
            albums.Total.ShouldBe(1);
            albums.PerPage.ShouldBe(25);
            albums.Data.Count.ShouldBe(1);
            albums.Paging.Next.ShouldBeNull();
            albums.Paging.Previous.ShouldBeNull();
            albums.Paging.First.ShouldBe($"/users/{VimeoSettings.PublicUserId}/albums?page=1");
            albums.Paging.Last.ShouldBe($"/users/{VimeoSettings.PublicUserId}/albums?page=1");
            var album = albums.Data[0];
            album.Name.ShouldBe("UnitTestAlbum");
            album.Description.ShouldBe("Simple album for testing purpose");
        }

        [Fact]
        public async Task AlbumManagementShouldWorkCorrectlyForMe()
        {
            // create a new album...
            const string originalName = "Unit Test Album";
            const string originalDesc =
                "This album was created via an automated test, and should be deleted momentarily...";
            const string password = "test";
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums",
                Method = RequestSettings.HttpMethod.Post,
                RequestTextBody = "privacy=password" +
                              "&sort=newest" +
                              $"&name={originalName.Replace(" ", "+")}" +
                              $"&description={originalDesc.Replace(" ", "+")
                                  .Replace(",", "%2C")}" +
                              $"&password={password}",
                ResponseJsonFile = "Album.create-album.json"
            });
            var client = CreateAuthenticatedClient();

            var newAlbum = await client.CreateAlbumAsync(UserId.Me, new EditAlbumParameters
            {
                Name = originalName,
                Description = originalDesc,
                Sort = EditAlbumSortOption.Newest,
                Privacy = EditAlbumPrivacyOption.Password,
                Password = password
            });

            newAlbum.ShouldNotBeNull();
            newAlbum.Name.ShouldBe(originalName);

            newAlbum.Description.ShouldBe(originalDesc);

            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums",
                ResponseJsonFile = "Album.albums.json"
            });

            // retrieve albums for the current user...there should be at least one now...
            var albums = await client.GetAlbumsAsync(UserId.Me);

            albums.Total.ShouldBeGreaterThan(0);

            // update the album...
            const string updatedName = "Unit Test Album (Updated)";
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums/10303859",
                Method = RequestSettings.HttpMethod.Patch,
                RequestTextBody = "privacy=anybody&name=Unit+Test+Album+%28Updated%29",
                ResponseJsonFile = "Album.patched-album.json"
            });
            var albumId = newAlbum.GetAlbumId();
            albumId.ShouldNotBeNull();
            var updatedAlbum = await client.UpdateAlbumAsync(UserId.Me, albumId.Value, new EditAlbumParameters
            {
                Name = updatedName,
                Privacy = EditAlbumPrivacyOption.Anybody
            });

            updatedAlbum.Name.ShouldBe(updatedName);

            // delete the album...
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums/10303859",
                Method = RequestSettings.HttpMethod.Delete
            });
            albumId = updatedAlbum.GetAlbumId();
            albumId.ShouldNotBeNull();
            var isDeleted = await client.DeleteAlbumAsync(UserId.Me, albumId.Value);

            isDeleted.ShouldBeTrue();
        }

        [Fact]
        public async Task AlbumManagementShouldWorkCorrectlyForUserId()
        {
            // create a new album...
            const string originalName = "Unit Test Album";
            const string originalDesc =
                "This album was created via an automated test, and should be deleted momentarily...";
            const string password = "test";
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/users/2433258/albums",
                Method = RequestSettings.HttpMethod.Post,
                RequestTextBody = "privacy=password" +
                              "&sort=newest" +
                              $"&name={originalName.Replace(" ", "+")}" +
                              $"&description={originalDesc.Replace(" ", "+")
                                  .Replace(",", "%2C")}" +
                              $"&password={password}",
                ResponseJsonFile = "Album.create-album.json"
            });

            var newAlbum = await AuthenticatedClient.CreateAlbumAsync(VimeoSettings.UserId, new EditAlbumParameters
            {
                Name = originalName,
                Description = originalDesc,
                Sort = EditAlbumSortOption.Newest,
                Privacy = EditAlbumPrivacyOption.Password,
                Password = "test"
            });

            newAlbum.ShouldNotBeNull();
            newAlbum.Name.ShouldBe(originalName);

            newAlbum.Description.ShouldBe(originalDesc);

            // retrieve albums for the user...there should be at least one now...
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/users/2433258/albums",
                ResponseJsonFile = "Album.albums.json"
            });
            var albums = await AuthenticatedClient.GetAlbumsAsync(VimeoSettings.UserId);

            albums.Total.ShouldBeGreaterThan(0);

            // update the album...
            const string updatedName = "Unit Test Album (Updated)";
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/users/2433258/albums/10303859", 
                Method = RequestSettings.HttpMethod.Patch,
                RequestTextBody = "privacy=anybody&name=Unit+Test+Album+%28Updated%29",
                ResponseJsonFile = "Album.patched-album.json"
            });
            var albumId = newAlbum.GetAlbumId();
            albumId.ShouldNotBeNull();
            var updatedAlbum = await AuthenticatedClient.UpdateAlbumAsync(VimeoSettings.UserId, albumId.Value,
                new EditAlbumParameters
                {
                    Name = updatedName,
                    Privacy = EditAlbumPrivacyOption.Anybody
                });

            updatedAlbum.Name.ShouldBe(updatedName);

            // delete the album...
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/users/2433258/albums/10303859",
                Method = RequestSettings.HttpMethod.Delete
            });
            albumId = updatedAlbum.GetAlbumId();
            albumId.ShouldNotBeNull();
            var isDeleted = await AuthenticatedClient.DeleteAlbumAsync(VimeoSettings.UserId, albumId.Value);

            isDeleted.ShouldBeTrue();
        }

        [Fact]
        public async Task GetAlbumsShouldCorrectlyWorkWithParameters()
        {
            MockHttpRequest(new RequestSettings
            {
                UrlSuffix = "/me/albums?per_page=50",
                ResponseJsonFile = "Album.album-with-params.json"
            });
            var client = CreateAuthenticatedClient();
            var albums = await client.GetAlbumsAsync(UserId.Me, new GetAlbumsParameters { PerPage = 50 });
            albums.ShouldNotBeNull();
            albums.PerPage.ShouldBe(50);
        }
    }
}