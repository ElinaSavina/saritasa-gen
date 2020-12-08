using EnvDTE;

namespace SaritasaGen.Domain
{
    /// <summary>
    /// Class model.
    /// </summary>
    public class ClassModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassModel" /> class.
        /// </summary>
        /// <param name="className">Class name.</param>
        /// <param name="classNamespace">Class namespace.</param>
        public ClassModel(string className, string classNamespace, Project project)
        {
            ClassName = className;
            ClassNamespace = classNamespace;
            ContainingProject = project;
        }

        /// <summary>
        /// Class name.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// Class namespace.
        /// </summary>
        public string ClassNamespace { get; }

        /// <summary>
        /// Project which contains class.
        /// </summary>
        public Project ContainingProject { get; }
    }
}
