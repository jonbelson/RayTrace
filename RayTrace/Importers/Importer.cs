using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Importers
{
    // Base class of results returned from a function call.
    public class TResult<T>
    {
        public T Result { get; init; }

        public List<string> Warnings { get; init; } = [];
        public List<string> Errors { get; init; } = [];
    }

    // Return type for import operations.
    public class ImportResult : TResult<Model>
    {
        public static ImportResult Success(Model model) => new() { Result = model };
        public static ImportResult Failure(List<string> errors) => new() { Errors = errors };
        public static ImportResult Failure(List<string> warnings, List<string> errors) => new() { Warnings = warnings, Errors = errors };
    }

    internal interface IImporter
    {
        public ModelData ImportModel(string fileName);

        //ImportResult DoImport() { return ImportResult.Success(new Model(new Scene(), new XMLModel())); }
        //ImportResult DoImport2() { return ImportResult.Failure(["oh", "noes"]); }
    }
}
