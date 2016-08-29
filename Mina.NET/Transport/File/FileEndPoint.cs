using System.IO;
using System.Net;

namespace Mina.Transport.File
{
    /// <summary>
    ///     文件终结点
    /// </summary>
    public class FileEndPoint : EndPoint
    {
        public FileEndPoint(string name, string path)
        {
            Path = path;
            Name = name;
            if (Directory.Exists(path))
                PathType = PathType.Directory;
            else if (System.IO.File.Exists(path))
                PathType = PathType.File;
            else
                PathType = PathType.NotExist;
        }

        /// <summary>
        ///     文件名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     文件路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     路径类型
        /// </summary>
        public PathType PathType { get; set; }
    }

    /// <summary>
    ///     路径类型
    /// </summary>
    public enum PathType
    {
        /// <summary>
        ///     目录或文件不存在
        /// </summary>
        NotExist,

        /// <summary>
        ///     目录
        /// </summary>
        Directory,

        /// <summary>
        ///     单个文件
        /// </summary>
        File
    }
}