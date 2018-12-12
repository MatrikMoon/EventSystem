using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/*
 * Created by Moon on 9/15/2018
 * Handles coroutines in parallel. You can "yield return ParallelCoroutine(params)" and
 * it will continue when all the provided coroutines have finished
 */

namespace ChristmasVotePlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    public class ParallelCoroutine
    {
        private int count;

        public IEnumerator ExecuteCoroutines(params IEnumerator[] coroutines)
        {
            count = coroutines.Length;
            coroutines.ToList().ForEach(x => SharedCoroutineStarter.instance.StartCoroutine(DoParallel(x)));
            yield return new WaitUntil(() => count == 0);
        }

        IEnumerator DoParallel(IEnumerator coroutine)
        {
            yield return SharedCoroutineStarter.instance.StartCoroutine(coroutine);
            count--;
        }
    }
}
