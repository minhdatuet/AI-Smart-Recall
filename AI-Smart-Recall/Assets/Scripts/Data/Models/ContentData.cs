using System;
using System.Collections.Generic;
using UnityEngine;

namespace AISmartRecall.Data.Models
{
    /// <summary>
    /// Model chứa thông tin nội dung học tập
    /// </summary>
    [Serializable]
    public class ContentData
    {
        [Header("Basic Info")]
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private string _content;
        [SerializeField] private ContentType _contentType;
        [SerializeField] private List<string> _tags = new List<string>();

        [Header("Statistics")]
        [SerializeField] private int _wordCount;
        [SerializeField] private int _estimatedReadingTime; // minutes
        [SerializeField] private DateTime _createdAt;

        // Properties
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Title
        {
            get => _title;
            set => _title = value;
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                UpdateWordCount();
            }
        }

        public ContentType ContentType
        {
            get => _contentType;
            set => _contentType = value;
        }

        public List<string> Tags
        {
            get => _tags;
            set => _tags = value ?? new List<string>();
        }

        public int WordCount => _wordCount;
        public int EstimatedReadingTime => _estimatedReadingTime;
        public DateTime CreatedAt => _createdAt;

        // Computed Properties
        public string FormattedCreatedAt => _createdAt.ToString("dd/MM/yyyy HH:mm");

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public ContentData()
        {
            _id = Guid.NewGuid().ToString();
            _title = "";
            _content = "";
            _contentType = ContentType.Understanding;
            _tags = new List<string>();
            _createdAt = DateTime.Now;
            UpdateWordCount();
        }

        /// <summary>
        /// Constructor với thông tin cơ bản
        /// </summary>
        public ContentData(string title, string content, ContentType contentType)
        {
            _id = Guid.NewGuid().ToString();
            _title = title;
            _content = content;
            _contentType = contentType;
            _tags = new List<string>();
            _createdAt = DateTime.Now;
            UpdateWordCount();
        }

        /// <summary>
        /// Cập nhật thống kê từ số
        /// </summary>
        private void UpdateWordCount()
        {
            if (string.IsNullOrEmpty(_content))
            {
                _wordCount = 0;
                _estimatedReadingTime = 0;
                return;
            }

            _wordCount = _content.Split(new char[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Estimate reading time (average 200 words per minute)
            _estimatedReadingTime = Mathf.CeilToInt(_wordCount / 200f);
            if (_estimatedReadingTime == 0) _estimatedReadingTime = 1;
        }

        /// <summary>
        /// Validate content có hợp lệ không
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_title) && 
                   !string.IsNullOrEmpty(_content) && 
                   _content.Length >= 50; // Minimum 50 characters
        }

        /// <summary>
        /// Get content summary (first 100 characters)
        /// </summary>
        public string GetSummary()
        {
            if (string.IsNullOrEmpty(_content))
                return "";

            return _content.Length > 100 ? 
                _content.Substring(0, 100) + "..." : 
                _content;
        }
    }

    /// <summary>
    /// Enum định nghĩa loại nội dung học tập
    /// </summary>
    public enum ContentType
    {
        /// <summary>
        /// Học thuộc lòng - phù hợp với từ vựng, công thức, định nghĩa
        /// </summary>
        Memorization,

        /// <summary>
        /// Học hiểu - phù hợp với đoạn văn, lý thuyết, phân tích
        /// </summary>
        Understanding
    }

    /// <summary>
    /// Extension methods cho ContentType
    /// </summary>
    public static class ContentTypeExtensions
    {
        public static string GetDisplayName(this ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Memorization => "Học thuộc",
                ContentType.Understanding => "Học hiểu",
                _ => "Không xác định"
            };
        }

        public static string GetDescription(this ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Memorization => "Phù hợp với từ vựng, công thức, định nghĩa, ngày tháng",
                ContentType.Understanding => "Phù hợp với đoạn văn, lý thuyết, giải thích, phân tích",
                _ => ""
            };
        }

        public static Color GetColor(this ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Memorization => new Color(1f, 0.7f, 0.3f), // Orange
                ContentType.Understanding => new Color(0.3f, 0.7f, 1f), // Blue
                _ => Color.gray
            };
        }

        public static string GetIcon(this ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Memorization => "[M]",
                ContentType.Understanding => "[U]", 
                _ => "[?]"
            };
        }
    }
}
