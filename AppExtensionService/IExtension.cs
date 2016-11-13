using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppExtensionService
{
    public interface IExtension<TIn, Tout, TProgress>
    {
        Task<Tout> Execute(TIn input, IProgress<TProgress> progress = null, CancellationToken cancelTokem = default(CancellationToken));
    }

    public interface IExtension<TIn, Tout, TOption, TProgress>
    {
        Task<Tout> Execute(TIn input, TOption options, IProgress<TProgress> progress = null, CancellationToken cancelTokem = default(CancellationToken));

        Task<TOption> PrototypeOptions { get; }

    }
}
