using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Renci.SshNet;

namespace Normaize.API.Services;

public class SftpStorageService : IStorageService
{
    private readonly string _host;
    private readonly string _username;
    private readonly string? _password;
    private readonly string? _privateKeyContent;
    private readonly string? _privateKeyPath;
    private readonly string _basePath;
    private readonly ILogger<SftpStorageService> _logger;

    public SftpStorageService(IConfiguration configuration, ILogger<SftpStorageService> logger)
    {
        _host = configuration["SFTP:Host"] ?? throw new ArgumentException("SFTP:Host configuration is required");
        _username = configuration["SFTP:Username"] ?? throw new ArgumentException("SFTP:Username configuration is required");
        _password = configuration["SFTP:Password"];
        _privateKeyContent = configuration["SFTP:PrivateKey"];
        _privateKeyPath = configuration["SFTP:PrivateKeyPath"];
        _basePath = configuration["SFTP:BasePath"] ?? "/uploads";
        _logger = logger;

        // Validate that either password or private key is provided
        if (string.IsNullOrEmpty(_password) && string.IsNullOrEmpty(_privateKeyContent) && string.IsNullOrEmpty(_privateKeyPath))
        {
            throw new ArgumentException("Either SFTP:Password, SFTP:PrivateKey, or SFTP:PrivateKeyPath must be provided");
        }
    }

    private SftpClient CreateSftpClient()
    {
        if (!string.IsNullOrEmpty(_privateKeyContent))
        {
            // Use private key content
            using var keyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_privateKeyContent));
            var keyFile = new PrivateKeyFile(keyStream);
            return new SftpClient(_host, _username, keyFile);
        }
        else if (!string.IsNullOrEmpty(_privateKeyPath))
        {
            // Use private key file
            var keyFile = new PrivateKeyFile(_privateKeyPath);
            return new SftpClient(_host, _username, keyFile);
        }
        else
        {
            // Use password authentication
            return new SftpClient(_host, _username, _password);
        }
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var remotePath = $"{_basePath}/{datePath}/{fileName}";
        
        using var client = CreateSftpClient();
        
        try
        {
            client.Connect();
            
            if (!client.IsConnected)
            {
                throw new Exception("Failed to connect to SFTP server");
            }

            // Create directory structure if it doesn't exist
            var directory = Path.GetDirectoryName(remotePath);
            if (!string.IsNullOrEmpty(directory) && !client.Exists(directory))
            {
                CreateDirectoryRecursive(client, directory);
            }

            // Upload file
            using var fileStream = client.Create(remotePath);
            await fileRequest.FileStream.CopyToAsync(fileStream);
            
            _logger.LogInformation("File uploaded to SFTP: {RemotePath}", remotePath);
            return $"sftp://{_host}{remotePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to SFTP: {RemotePath}", remotePath);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        // Extract path from sftp:// URL
        var remotePath = ExtractPathFromUrl(filePath);
        
        using var client = CreateSftpClient();
        
        try
        {
            client.Connect();
            
            if (!client.IsConnected)
            {
                throw new Exception("Failed to connect to SFTP server");
            }

            if (!client.Exists(remotePath))
            {
                throw new FileNotFoundException($"File not found on SFTP server: {remotePath}");
            }

            var memoryStream = new MemoryStream();
            client.DownloadFile(remotePath, memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from SFTP: {RemotePath}", remotePath);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var remotePath = ExtractPathFromUrl(filePath);
        
        using var client = CreateSftpClient();
        
        try
        {
            client.Connect();
            
            if (!client.IsConnected)
            {
                throw new Exception("Failed to connect to SFTP server");
            }

            if (client.Exists(remotePath))
            {
                client.DeleteFile(remotePath);
                _logger.LogInformation("File deleted from SFTP: {RemotePath}", remotePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from SFTP: {RemotePath}", remotePath);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        var remotePath = ExtractPathFromUrl(filePath);
        
        using var client = CreateSftpClient();
        
        try
        {
            client.Connect();
            
            if (!client.IsConnected)
            {
                return false;
            }

            return client.Exists(remotePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence on SFTP: {RemotePath}", remotePath);
            return false;
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    private void CreateDirectoryRecursive(SftpClient client, string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentPath = "";
        
        foreach (var part in parts)
        {
            currentPath += "/" + part;
            if (!client.Exists(currentPath))
            {
                client.CreateDirectory(currentPath);
            }
        }
    }

    private string ExtractPathFromUrl(string filePath)
    {
        if (filePath.StartsWith("sftp://"))
        {
            var uri = new Uri(filePath);
            return uri.AbsolutePath;
        }
        return filePath;
    }
} 