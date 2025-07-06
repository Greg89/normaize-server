import React, { useState, useCallback, useRef } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, File, X, CheckCircle, AlertCircle, Loader } from 'lucide-react';

interface FileUploadProps {
  onUploadSuccess: (datasetId: number, fileName: string) => void;
  onUploadError: (error: string) => void;
  maxFileSize?: number;
  allowedTypes?: string[];
  multiple?: boolean;
}

interface UploadProgress {
  fileName: string;
  progress: number;
  status: 'uploading' | 'success' | 'error';
  error?: string;
  dataSetId?: number;
}

const FileUpload: React.FC<FileUploadProps> = ({
  onUploadSuccess,
  onUploadError,
  maxFileSize = 100 * 1024 * 1024, // 100MB
  allowedTypes = ['.csv', '.json', '.xlsx', '.xls', '.xml', '.txt'],
  multiple = false
}) => {
  const [uploads, setUploads] = useState<UploadProgress[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    const newUploads = acceptedFiles.map(file => ({
      fileName: file.name,
      progress: 0,
      status: 'uploading' as const
    }));

    setUploads(prev => [...prev, ...newUploads]);

    for (let i = 0; i < acceptedFiles.length; i++) {
      const file = acceptedFiles[i];
      await uploadFile(file, newUploads.length - acceptedFiles.length + i);
    }
  }, []);

  const uploadFile = async (file: File, uploadIndex: number) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('name', file.name.replace(/\.[^/.]+$/, '')); // Remove extension
    formData.append('description', `Uploaded on ${new Date().toLocaleString()}`);

    abortControllerRef.current = new AbortController();

    try {
      const response = await fetch('/api/datasets/upload', {
        method: 'POST',
        body: formData,
        signal: abortControllerRef.current.signal,
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });

      if (!response.ok) {
        throw new Error(`Upload failed: ${response.statusText}`);
      }

      const result = await response.json();
      
      if (result.success) {
        setUploads(prev => prev.map((upload, index) => 
          index === uploadIndex 
            ? { ...upload, progress: 100, status: 'success', dataSetId: result.dataSetId }
            : upload
        ));
        
        onUploadSuccess(result.dataSetId, file.name);
      } else {
        throw new Error(result.message || 'Upload failed');
      }
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        return; // Upload was cancelled
      }
      
      setUploads(prev => prev.map((upload, index) => 
        index === uploadIndex 
          ? { ...upload, status: 'error', error: error instanceof Error ? error.message : 'Upload failed' }
          : upload
      ));
      
      onUploadError(error instanceof Error ? error.message : 'Upload failed');
    }
  };

  const removeUpload = (index: number) => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setUploads(prev => prev.filter((_, i) => i !== index));
  };

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: allowedTypes.reduce((acc, type) => {
      acc[type] = [];
      return acc;
    }, {} as Record<string, string[]>),
    maxSize: maxFileSize,
    multiple
  });

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getFileIcon = (fileName: string) => {
    const extension = fileName.split('.').pop()?.toLowerCase();
    switch (extension) {
      case 'csv':
        return '📊';
      case 'json':
        return '📄';
      case 'xlsx':
      case 'xls':
        return '📈';
      case 'xml':
        return '📋';
      case 'txt':
        return '📝';
      default:
        return '📁';
    }
  };

  return (
    <div className="w-full max-w-4xl mx-auto">
      {/* Drop Zone */}
      <div
        {...getRootProps()}
        className={`
          border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors
          ${isDragActive 
            ? 'border-blue-500 bg-blue-50' 
            : 'border-gray-300 hover:border-gray-400'
          }
        `}
        onDragEnter={() => setIsDragging(true)}
        onDragLeave={() => setIsDragging(false)}
      >
        <input {...getInputProps()} />
        <Upload className="mx-auto h-12 w-12 text-gray-400 mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {isDragActive ? 'Drop files here' : 'Drag & drop files here'}
        </h3>
        <p className="text-gray-500 mb-4">
          or click to select files
        </p>
        <div className="text-sm text-gray-400">
          <p>Supported formats: {allowedTypes.join(', ')}</p>
          <p>Max file size: {formatFileSize(maxFileSize)}</p>
        </div>
      </div>

      {/* Upload Progress */}
      {uploads.length > 0 && (
        <div className="mt-6 space-y-4">
          <h3 className="text-lg font-medium text-gray-900">Upload Progress</h3>
          {uploads.map((upload, index) => (
            <div
              key={index}
              className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center space-x-3">
                  <span className="text-2xl">{getFileIcon(upload.fileName)}</span>
                  <div>
                    <p className="font-medium text-gray-900">{upload.fileName}</p>
                    <p className="text-sm text-gray-500">
                      {upload.status === 'uploading' && 'Uploading...'}
                      {upload.status === 'success' && 'Upload complete'}
                      {upload.status === 'error' && 'Upload failed'}
                    </p>
                  </div>
                </div>
                <div className="flex items-center space-x-2">
                  {upload.status === 'uploading' && (
                    <Loader className="h-5 w-5 text-blue-500 animate-spin" />
                  )}
                  {upload.status === 'success' && (
                    <CheckCircle className="h-5 w-5 text-green-500" />
                  )}
                  {upload.status === 'error' && (
                    <AlertCircle className="h-5 w-5 text-red-500" />
                  )}
                  <button
                    onClick={() => removeUpload(index)}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    <X className="h-5 w-5" />
                  </button>
                </div>
              </div>

              {/* Progress Bar */}
              {upload.status === 'uploading' && (
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${upload.progress}%` }}
                  />
                </div>
              )}

              {/* Error Message */}
              {upload.status === 'error' && upload.error && (
                <p className="text-sm text-red-600 mt-2">{upload.error}</p>
              )}

              {/* Success Message */}
              {upload.status === 'success' && upload.dataSetId && (
                <p className="text-sm text-green-600 mt-2">
                  Dataset ID: {upload.dataSetId}
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      {/* File Type Information */}
      <div className="mt-6 bg-gray-50 rounded-lg p-4">
        <h4 className="font-medium text-gray-900 mb-2">Supported File Formats</h4>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
          <div className="flex items-center space-x-2">
            <span>📊</span>
            <span>CSV - Comma-separated values</span>
          </div>
          <div className="flex items-center space-x-2">
            <span>📄</span>
            <span>JSON - JavaScript Object Notation</span>
          </div>
          <div className="flex items-center space-x-2">
            <span>📈</span>
            <span>Excel - XLSX/XLS files</span>
          </div>
          <div className="flex items-center space-x-2">
            <span>📋</span>
            <span>XML - Extensible Markup Language</span>
          </div>
          <div className="flex items-center space-x-2">
            <span>📝</span>
            <span>TXT - Plain text files</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FileUpload; 