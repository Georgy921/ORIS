using System;
using System.IO;
using System.Linq;

namespace MiniHttpServer.Shared
{
    public class GetResponseBytes 
    {
        private const string PUBLIC_FOLDER = "Public";
        
        public static byte[]? Invoke(string path) // index.html or style.css
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("⚠️  Empty path, serving index.html");
                return TryGetFile("index.html");
            }

            // If path has extension, it's a file
            if (Path.HasExtension(path)) // index.html or style.css
            {
                Console.WriteLine(path);
                return TryGetFile(path);
            }
    
            // No extension - check if it's a directory image, path = "make"
            string directoryPath = Path.Combine(PUBLIC_FOLDER, path); // public/make
            if (Directory.Exists(directoryPath))
            {
                // Directory exists, try its index.html
                string indexPath = Path.Combine(path, "index.html"); // Public/index.html
                Console.WriteLine($"📁 Directory '{path}' found, trying {indexPath}");
                return TryGetFile(indexPath);
            }
    
            // Not a file, not a directory - try as filename anyway (fallback)
            Console.WriteLine($"⚠️  '{path}' has no extension and is not a directory, trying as-is"); // path = "make" - doesn't have extension, no directory exists public/make
            return TryGetFile(path); // public/make - trouble??
        }
        
        private static byte[]? TryGetFile(string path)
        {
            try
            {
                // Normalize path separators
                string normalizedPath = path.Replace('/', Path.DirectorySeparatorChar) // auth\index.html -> auth/index.html
                                            .Replace('\\', Path.DirectorySeparatorChar);
                
                // Remove leading separator if present
                normalizedPath = normalizedPath.TrimStart(Path.DirectorySeparatorChar); // auth/index.html or auth/style.css

                // Security: Prevent directory traversal attacks
                if (normalizedPath.Contains(".."))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"🚫 Security: Directory traversal attempt blocked: {path}");
                    Console.ResetColor();
                    return null;
                }

                // Try direct path first (most common case)
                string directPath = Path.Combine(PUBLIC_FOLDER, normalizedPath); // public/auth/index.html or public/auth/style.css
                if (File.Exists(directPath)) // if such one exists - public/auth/index.html
                {
                    return File.ReadAllBytes(directPath);
                }
                // trying to find in public directory
                
                // If not found directly, search in subdirectories
                string fileName = Path.GetFileName(normalizedPath); // index.html or style.css
                string? targetPath = null;

                // Build the target path pattern to match
                string[] pathParts = normalizedPath.Split(Path.DirectorySeparatorChar); // [public, auth, index.html]
                
                if (!Directory.Exists(PUBLIC_FOLDER)) // trying to find in public directory, but it doesn't exists
                {
                    return null;
                }
                // public directory exists 
                
                // Search for file recursively
                var matchingFiles = Directory.EnumerateFiles(
                    PUBLIC_FOLDER, // it guaranteed exists
                    fileName, // index.html or style.css
                    SearchOption.AllDirectories 
                ); 

                // Try to find exact path match
                foreach (var file in matchingFiles) // ...public/index.html , ...public/auth/index.html , ...public/instances/index.html and others (full_path)...index.html
                {
                    // Check if this file matches the requested path structure
                    if (file.EndsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)) // full path ends with required auth/index.html - only one such file
                    {
                        targetPath = file; // OstapMamykin...public/auth/index.html - the second one
                        break;
                    }
                }

                // If exact match not found, take first file with matching name
                if (targetPath == null) // no files ended with auth/index.html
                {
                    targetPath = matchingFiles.FirstOrDefault(); // but maybe some or just one with matching ...index.html at the end - file name equals
                }

                if (targetPath == null) // no such files (...index.html) found at all
                {
                    return null;
                }

                // Read and return file contents
                byte[] fileBytes = File.ReadAllBytes(targetPath); // targetPath - full path to the required file
                
                return fileBytes;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Directory not found: {ex.Message}");
                Console.ResetColor();
                return null;
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  File not found: {ex.Message}");
                Console.ResetColor();
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"🚫 Access denied: {ex.Message}");
                Console.ResetColor();
                return null;
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ IO Error reading file: {ex.Message}");
                Console.ResetColor();
                return null;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Unexpected error reading file: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }
        
        
    }
}