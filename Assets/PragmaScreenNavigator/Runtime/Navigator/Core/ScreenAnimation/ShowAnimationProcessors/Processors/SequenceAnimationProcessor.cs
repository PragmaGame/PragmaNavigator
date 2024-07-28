using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Pragma.ScreenNavigator
{
    public class SequenceAnimationProcessor : IShowAnimationProcessor
    {
        public async UniTask<bool> Show(IEnumerable<UniTask<bool>> tasks)
        {
            foreach (var task in tasks)
            {
                if (!await task)
                {
                    return false;
                }
            }

            return true;
        }
    }
}