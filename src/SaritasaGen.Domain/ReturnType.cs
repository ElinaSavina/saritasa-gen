namespace SaritasaGen.Domain
{
    /// <summary>
    /// Return type.
    /// </summary>
    public class ReturnType
    {
        /// <summary>
        /// Type name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// True if type is built-in.
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnType" /> class.
        /// </summary>
        /// <param name="name">Type name.</param>
        /// <param name="typeNamespace">Type namespace.</param>
        /// <param name="builtIn">True if type is built-in.</param>
        public ReturnType(string name, string typeNamespace, bool builtIn)
        {
            Name = name;
            Namespace = typeNamespace;
            IsBuiltIn = builtIn;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnType" /> class.
        /// </summary>
        public ReturnType()
        {
        }
    }
}
