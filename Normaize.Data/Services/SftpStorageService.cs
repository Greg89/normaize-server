using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Renci.SshNet;

namespace Normaize.Data.Services;

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
        _basePath = configuration["SFTP:BasePath"] ?? "/srv/sftpgo/data";
        _logger = logger;

        // Log configuration (without sensitive data)
        _logger.LogInformation("SFTP Storage Service initialized with Host: {Host}, Username: {Username}, BasePath: {BasePath}", 
            _host, _username, _basePath);

        // Validate that either password or private key is provided
        if (string.IsNullOrEmpty(_password) && string.IsNullOrEmpty(_privateKeyContent) && string.IsNullOrEmpty(_privateKeyPath))
        {
            throw new ArgumentException("Either SFTP:Password, SFTP:PrivateKey, or SFTP:PrivateKeyPath must be provided");
        }

        _logger.LogInformation("SFTP authentication method: {Method}", 
            !string.IsNullOrEmpty(_password) ? "Password" : "Private Key");
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
            return new SftpClient(_host, _username, _password ?? string.Empty);
        }
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var remotePath = $"{_basePath}/{datePath}/{fileName}";
        
        _logger.LogInformation("Attempting to upload file {FileName} to SFTP path {RemotePath}", 
            fileRequest.FileName, remotePath);
        
        using var client = CreateSftpClient();
        
        try
        {
            _logger.LogInformation("Connecting to SFTP server {Host} as user {Username}", _host, _username);
            client.Connect();
            
            if (!client.IsConnected)
            {
                var error = "Failed to connect to SFTP server";
                _logger.LogError(error);
                throw new Exception(error);
            }

            _logger.LogInformation("Successfully connected to SFTP server");

            // Create directory structure if it doesn't exist
            var directory = Path.GetDirectoryName(remotePath);
            if (!string.IsNullOrEmpty(directory) && !client.Exists(directory))
            {
                _logger.LogInformation("Creating directory structure: {Directory}", directory);
                CreateDirectoryRecursive(client, directory);
            }

            // Upload file
            _logger.LogInformation("Uploading file {FileName} ({FileSize} bytes)", 
                fileRequest.FileName, fileRequest.FileSize);
            
            using var fileStream = client.Create(remotePath);
            await fileRequest.FileStream.CopyToAsync(fileStream);
            
            _logger.LogInformation("File uploaded successfully to SFTP: {RemotePath}", remotePath);
            return $"sftp://{_host}{remotePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to SFTP: {RemotePath}. Host: {Host}, Username: {Username}", 
                remotePath, _host, _username);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                _logger.LogInformation("Disconnecting from SFTP server");
                client.Disconnect();
            }
        }
    }

    public Task<Stream> GetFileAsync(string filePath)
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
            
            return Task.FromResult<Stream>(memoryStream);
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

    public Task DeleteFileAsync(string filePath)
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

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var remotePath = ExtractPathFromUrl(filePath);
        
        using var client = CreateSftpClient();
        
        try
        {
            client.Connect();
            
            if (!client.IsConnected)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(client.Exists(remotePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence on SFTP: {RemotePath}", remotePath);
            return Task.FromResult(false);
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