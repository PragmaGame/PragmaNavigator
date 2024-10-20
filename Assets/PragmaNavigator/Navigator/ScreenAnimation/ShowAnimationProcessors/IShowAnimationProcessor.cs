using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Pragma.Navigator
{
    public interface IShowAnimationProcessor
    {
        public UniTask<bool> Show(IEnumerable<UniTask<bool>> tasks);
    }
}