using Octokit;
using System.Text;
using System.Text.Json;

namespace GitHubMcpServer.Tools
{
    public interface IGitHubTool
    {
        string Name { get; }
        string Description { get; }
        object InputSchema { get; }
        Task<object> ExecuteAsync(Dictionary<string, object> arguments);
    }

    public abstract class GitHubToolBase : IGitHubTool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract object InputSchema { get; }
        public abstract Task<object> ExecuteAsync(Dictionary<string, object> arguments);

        protected string GetStringArgument(Dictionary<string, object> arguments, string key, string defaultValue = "")
        {
            if (!arguments.ContainsKey(key))
                return defaultValue;

            var value = arguments[key];
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind == System.Text.Json.JsonValueKind.String ? jsonElement.GetString() ?? defaultValue : defaultValue;
            }
            return value?.ToString() ?? defaultValue;
        }

        protected bool GetBooleanArgument(Dictionary<string, object> arguments, string key, bool defaultValue = false)
        {
            if (!arguments.ContainsKey(key))
                return defaultValue;

            var value = arguments[key];
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind == System.Text.Json.JsonValueKind.True || 
                       (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String && 
                        bool.TryParse(jsonElement.GetString(), out var boolResult) && boolResult);
            }
            
            if (value is bool boolValue)
                return boolValue;
                
            if (bool.TryParse(value?.ToString(), out var parsedBool))
                return parsedBool;
                
            return defaultValue;
        }

        protected int GetIntegerArgument(Dictionary<string, object> arguments, string key, int defaultValue = 0)
        {
            if (!arguments.ContainsKey(key))
                return defaultValue;

            var value = arguments[key];
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                    return jsonElement.GetInt32();
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String && 
                    int.TryParse(jsonElement.GetString(), out var intResult))
                    return intResult;
            }
            
            if (value is int intValue)
                return intValue;
                
            if (int.TryParse(value?.ToString(), out var parsedInt))
                return parsedInt;
                
            return defaultValue;
        }
    }

    public class GitHubRepositoryAnalyzerTool : GitHubToolBase
    {
        private readonly GitHubClient _gitHubClient;

        public override string Name => "github_analyze_repository";
        public override string Description => "Analyze a GitHub repository and provide a summary of its structure, technologies, and key files";

        public override object InputSchema => new
        {
            type = "object",
            properties = new
            {
                owner = new
                {
                    type = "string",
                    description = "Repository owner (username or organization)"
                },
                repo = new
                {
                    type = "string",
                    description = "Repository name"
                }
            },
            required = new[] { "owner", "repo" }
        };

        public GitHubRepositoryAnalyzerTool(GitHubClient gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
        {
            try
            {
                var owner = GetStringArgument(arguments, "owner");
                var repo = GetStringArgument(arguments, "repo");

                // Get repository information
                var repository = await _gitHubClient.Repository.Get(owner, repo);
                
                // Get repository contents (root level)
                var contents = await _gitHubClient.Repository.Content.GetAllContents(owner, repo);
                
                // Get languages
                var languages = await _gitHubClient.Repository.GetAllLanguages(owner, repo);
                
                // Get README content if exists
                string? readmeContent = null;
                try
                {
                    var readmeFiles = contents.Where(c => c.Name.ToLower().StartsWith("readme")).ToList();
                    if (readmeFiles.Any())
                    {
                        var readme = await _gitHubClient.Repository.Content.GetRawContent(owner, repo, readmeFiles.First().Path);
                        readmeContent = Encoding.UTF8.GetString(readme);
                    }
                }
                catch
                {
                    // README might not exist or be accessible
                }

                // Analyze project structure
                var projectStructure = AnalyzeProjectStructure(contents);

                var summary = new
                {
                    repository = new
                    {
                        name = repository.Name,
                        fullName = repository.FullName,
                        description = repository.Description,
                        url = repository.HtmlUrl,
                        stars = repository.StargazersCount,
                        forks = repository.ForksCount,
                        language = repository.Language,
                        createdAt = repository.CreatedAt,
                        updatedAt = repository.UpdatedAt,
                        size = repository.Size
                    },
                    languages = languages.ToDictionary(l => l.Name, l => l.NumberOfBytes),
                    projectStructure = projectStructure,
                    keyFiles = GetKeyFiles(contents),
                    readmeContent = readmeContent?.Length > 2000 ? readmeContent.Substring(0, 2000) + "..." : readmeContent
                };

                return summary;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to analyze repository: {ex.Message}");
            }
        }

        private object AnalyzeProjectStructure(IReadOnlyList<RepositoryContent> contents)
        {
            var directories = contents.Where(c => c.Type == ContentType.Dir).Select(c => c.Name).ToList();
            var files = contents.Where(c => c.Type == ContentType.File).Select(c => c.Name).ToList();

            // Detect project type based on files
            var projectType = DetectProjectType(files);
            
            return new
            {
                projectType = projectType,
                directories = directories,
                rootFiles = files,
                totalItems = contents.Count
            };
        }

        private string DetectProjectType(List<string> files)
        {
            if (files.Any(f => f.EndsWith(".csproj") || f.EndsWith(".sln"))) return ".NET/C#";
            if (files.Contains("package.json")) return "Node.js/JavaScript";
            if (files.Contains("requirements.txt") || files.Contains("setup.py") || files.Contains("pyproject.toml")) return "Python";
            if (files.Contains("Cargo.toml")) return "Rust";
            if (files.Contains("go.mod")) return "Go";
            if (files.Contains("pom.xml") || files.Contains("build.gradle")) return "Java";
            if (files.Contains("Gemfile")) return "Ruby";
            if (files.Contains("composer.json")) return "PHP";
            
            return "Unknown";
        }

        private List<string> GetKeyFiles(IReadOnlyList<RepositoryContent> contents)
        {
            var keyFilePatterns = new[]
            {
                "readme", "license", "changelog", "contributing", "dockerfile", "makefile",
                ".gitignore", "package.json", "requirements.txt", "setup.py", "cargo.toml",
                "go.mod", "pom.xml", "build.gradle", "composer.json", "gemfile"
            };

            return contents
                .Where(c => c.Type == ContentType.File)
                .Where(c => keyFilePatterns.Any(pattern => c.Name.ToLower().Contains(pattern.ToLower())))
                .Select(c => c.Name)
                .ToList();
        }
    }

    public class GitHubPullRequestReviewerTool : GitHubToolBase
    {
        private readonly GitHubClient _gitHubClient;

        public override string Name => "github_review_pull_request";
        public override string Description => "Review a GitHub pull request and provide analysis of changes, files modified, and suggestions";

        public override object InputSchema => new
        {
            type = "object",
            properties = new
            {
                owner = new
                {
                    type = "string",
                    description = "Repository owner (username or organization)"
                },
                repo = new
                {
                    type = "string",
                    description = "Repository name"
                },
                pullNumber = new
                {
                    type = "integer",
                    description = "Pull request number"
                }
            },
            required = new[] { "owner", "repo", "pullNumber" }
        };

        public GitHubPullRequestReviewerTool(GitHubClient gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
        {
            try
            {
                var owner = GetStringArgument(arguments, "owner");
                var repo = GetStringArgument(arguments, "repo");
                var pullNumber = GetIntegerArgument(arguments, "pullNumber");

                // Get pull request details
                var pullRequest = await _gitHubClient.PullRequest.Get(owner, repo, pullNumber);
                
                // Get pull request files
                var files = await _gitHubClient.PullRequest.Files(owner, repo, pullNumber);
                
                // Get pull request comments
                var comments = await _gitHubClient.PullRequest.ReviewComment.GetAll(owner, repo, pullNumber);
                
                // Get commits
                var commits = await _gitHubClient.PullRequest.Commits(owner, repo, pullNumber);

                // Analyze changes
                var changeAnalysis = AnalyzeChanges(files);

                var review = new
                {
                    pullRequest = new
                    {
                        number = pullRequest.Number,
                        title = pullRequest.Title,
                        description = pullRequest.Body,
                        state = pullRequest.State.ToString(),
                        author = pullRequest.User.Login,
                        createdAt = pullRequest.CreatedAt,
                        updatedAt = pullRequest.UpdatedAt,
                        mergeable = pullRequest.Mergeable,
                        additions = pullRequest.Additions,
                        deletions = pullRequest.Deletions,
                        changedFiles = pullRequest.ChangedFiles,
                        url = pullRequest.HtmlUrl
                    },
                    commits = commits.Select(c => new
                    {
                        sha = c.Sha,
                        message = c.Commit.Message,
                        author = c.Commit.Author.Name,
                        date = c.Commit.Author.Date
                    }).ToList(),
                    filesChanged = files.Select(f => new
                    {
                        filename = f.FileName,
                        status = f.Status,
                        additions = f.Additions,
                        deletions = f.Deletions,
                        changes = f.Changes,
                        patch = f.Patch?.Length > 1000 ? f.Patch.Substring(0, 1000) + "..." : f.Patch
                    }).ToList(),
                    changeAnalysis = changeAnalysis,
                    existingComments = comments.Count(),
                    reviewSuggestions = GenerateReviewSuggestions(pullRequest, files, changeAnalysis)
                };

                return review;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to review pull request: {ex.Message}");
            }
        }

        private object AnalyzeChanges(IReadOnlyList<PullRequestFile> files)
        {
            var fileTypes = files.GroupBy(f => Path.GetExtension(f.FileName))
                .ToDictionary(g => g.Key, g => g.Count());

            var totalAdditions = files.Sum(f => f.Additions);
            var totalDeletions = files.Sum(f => f.Deletions);

            var modifiedDirectories = files.Select(f => Path.GetDirectoryName(f.FileName))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .ToList();

            return new
            {
                totalFiles = files.Count,
                totalAdditions = totalAdditions,
                totalDeletions = totalDeletions,
                netChanges = totalAdditions - totalDeletions,
                fileTypeBreakdown = fileTypes,
                modifiedDirectories = modifiedDirectories,
                largestFiles = files.OrderByDescending(f => f.Changes).Take(5)
                    .Select(f => new { filename = f.FileName, changes = f.Changes }).ToList()
            };
        }

        private List<string> GenerateReviewSuggestions(PullRequest pullRequest, IReadOnlyList<PullRequestFile> files, object changeAnalysis)
        {
            var suggestions = new List<string>();

            // Size-based suggestions
            if (files.Count > 20)
            {
                suggestions.Add("This PR modifies a large number of files. Consider breaking it into smaller, focused PRs for easier review.");
            }

            if (files.Sum(f => f.Changes) > 500)
            {
                suggestions.Add("This PR has a large number of changes. Ensure all changes are related and necessary.");
            }

            // Title and description suggestions
            if (string.IsNullOrWhiteSpace(pullRequest.Body))
            {
                suggestions.Add("Consider adding a description to explain the purpose and scope of these changes.");
            }

            // File type suggestions
            var hasTests = files.Any(f => f.FileName.Contains("test", StringComparison.OrdinalIgnoreCase) || 
                                        f.FileName.Contains("spec", StringComparison.OrdinalIgnoreCase));
            if (!hasTests && files.Any(f => !f.FileName.Contains("test", StringComparison.OrdinalIgnoreCase)))
            {
                suggestions.Add("Consider adding or updating tests for the changes made.");
            }

            return suggestions;
        }
    }

    public class GitHubRepositoryContentsTool : GitHubToolBase
    {
        private readonly GitHubClient _gitHubClient;

        public override string Name => "github_get_repository_contents";
        public override string Description => "Get the raw contents of files and directories from a GitHub repository for client-side analysis";

        public override object InputSchema => new
        {
            type = "object",
            properties = new
            {
                owner = new
                {
                    type = "string",
                    description = "Repository owner (username or organization)"
                },
                repo = new
                {
                    type = "string",
                    description = "Repository name"
                },
                path = new
                {
                    type = "string",
                    description = "Path within the repository (empty string or '.' for root)",
                    @default = ""
                },
                recursive = new
                {
                    type = "boolean",
                    description = "Whether to recursively fetch contents of subdirectories",
                    @default = false
                },
                includeContent = new
                {
                    type = "boolean",
                    description = "Whether to include file contents (for text files only)",
                    @default = true
                },
                maxFileSize = new
                {
                    type = "integer",
                    description = "Maximum file size in bytes to include content for (default 100KB)",
                    @default = 102400
                }
            },
            required = new[] { "owner", "repo" }
        };

        public GitHubRepositoryContentsTool(GitHubClient gitHubClient)
        {
            _gitHubClient = gitHubClient;
        }

        public override async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
        {
            try
            {
                var owner = GetStringArgument(arguments, "owner");
                var repo = GetStringArgument(arguments, "repo");
                var path = GetStringArgument(arguments, "path", "");
                var recursive = GetBooleanArgument(arguments, "recursive", false);
                var includeContent = GetBooleanArgument(arguments, "includeContent", true);
                var maxFileSize = GetIntegerArgument(arguments, "maxFileSize", 102400);

                var result = new
                {
                    repository = new { owner, name = repo, path },
                    contents = await GetContentsRecursive(owner, repo, path, recursive, includeContent, maxFileSize)
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get repository contents: {ex.Message}");
            }
        }

        private async Task<List<object>> GetContentsRecursive(string owner, string repo, string path, bool recursive, bool includeContent, int maxFileSize)
        {
            var results = new List<object>();

            try
            {
                IReadOnlyList<RepositoryContent> contents;
                if (string.IsNullOrEmpty(path) || path == ".")
                {
                    contents = await _gitHubClient.Repository.Content.GetAllContents(owner, repo);
                }
                else
                {
                    contents = await _gitHubClient.Repository.Content.GetAllContents(owner, repo, path);
                }

                foreach (var item in contents)
                {
                    var itemResult = new Dictionary<string, object>
                    {
                        ["name"] = item.Name,
                        ["path"] = item.Path,
                        ["type"] = item.Type.ToString().ToLower(),
                        ["size"] = item.Size,
                        ["sha"] = item.Sha,
                        ["url"] = item.HtmlUrl,
                        ["downloadUrl"] = item.DownloadUrl ?? ""
                    };

                    if (item.Type == ContentType.File && includeContent)
                    {
                        // Only include content for reasonably sized text files
                        if (item.Size <= maxFileSize && IsLikelyTextFile(item.Name))
                        {
                            try
                            {
                                var fileContent = await _gitHubClient.Repository.Content.GetRawContent(owner, repo, item.Path);
                                var contentText = Encoding.UTF8.GetString(fileContent);
                                
                                // Verify it's actually text (not binary)
                                if (IsValidUtf8Text(contentText))
                                {
                                    itemResult["content"] = contentText;
                                }
                                else
                                {
                                    itemResult["content"] = "[Binary file - content not included]";
                                }
                            }
                            catch
                            {
                                itemResult["content"] = "[Could not retrieve file content]";
                            }
                        }
                        else if (item.Size > maxFileSize)
                        {
                            itemResult["content"] = $"[File too large ({item.Size} bytes) - content not included]";
                        }
                        else
                        {
                            itemResult["content"] = "[Binary file - content not included]";
                        }
                    }

                    results.Add(itemResult);

                    // Recursively get directory contents if requested
                    if (recursive && item.Type == ContentType.Dir)
                    {
                        try
                        {
                            var subContents = await GetContentsRecursive(owner, repo, item.Path, recursive, includeContent, maxFileSize);
                            itemResult["children"] = subContents;
                        }
                        catch (Exception ex)
                        {
                            itemResult["children"] = new List<object>();
                            itemResult["error"] = $"Could not access directory: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new Dictionary<string, object>
                {
                    ["error"] = $"Could not access path '{path}': {ex.Message}",
                    ["path"] = path
                });
            }

            return results;
        }

        private bool IsLikelyTextFile(string filename)
        {
            var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".txt", ".md", ".markdown", ".json", ".xml", ".yml", ".yaml", ".toml",
                ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".hpp",
                ".rb", ".php", ".go", ".rs", ".swift", ".kt", ".scala", ".clj",
                ".html", ".htm", ".css", ".scss", ".sass", ".less", ".vue", ".jsx", ".tsx",
                ".sql", ".sh", ".bat", ".ps1", ".cmd", ".dockerfile", ".gitignore",
                ".gitattributes", ".editorconfig", ".env", ".ini", ".cfg", ".conf",
                ".log", ".csv", ".tsv", ".r", ".R", ".m", ".pl", ".lua", ".vim"
            };

            var extension = Path.GetExtension(filename);
            if (textExtensions.Contains(extension))
                return true;

            // Files without extensions that are commonly text
            var textFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "README", "LICENSE", "CHANGELOG", "CONTRIBUTING", "AUTHORS", "COPYING",
                "INSTALL", "NEWS", "TODO", "MANIFEST", "Makefile", "Dockerfile",
                "Jenkinsfile", "Vagrantfile", "Gemfile", "Rakefile", "Procfile"
            };

            return textFilenames.Contains(Path.GetFileNameWithoutExtension(filename)) ||
                   textFilenames.Contains(filename);
        }

        private bool IsValidUtf8Text(string content)
        {
            // Simple heuristic: if the content contains too many null bytes or non-printable characters,
            // it's likely binary
            if (content.Length == 0) return true;

            int nullCount = content.Count(c => c == '\0');
            int nonPrintableCount = content.Count(c => c < 32 && c != '\r' && c != '\n' && c != '\t');

            // If more than 1% null bytes or more than 5% non-printable, consider it binary
            return (nullCount < content.Length * 0.01) && (nonPrintableCount < content.Length * 0.05);
        }
    }
}
