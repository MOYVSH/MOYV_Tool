using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AC.Strcuts;

/// <summary>
/// AnimationComposer是一个用来协同多个动画对象以及动态子对象的动画控制系统。
/// 在很多情况下，比如一个UI的窗口的展现动画，需要界面的根对象动画，以及一系列子对象的动画相互配合组成。
/// AnimationComposer可以为这种情况提供支持。
/// 控制命令(Command):
///     Command用来控制指定对象或子对象的动画播放.
/// 控制命令序列(CommandSequence):
///     CommandSequence包含了一个顺序执行的控制命令序列。AnimationComposer可以指定执行一个CommandSequence。
/// AnimationComposer可以像播放一个普通动画一样播放一个CommandSequence，来达成一个最终的整体动画效果。
/// </summary>
/// 
public class AnimationComposer : MonoBehaviour
{
    /// <summary>
    /// 所有的命令序列.
    /// </summary>
    public List<CommandSequence> sequences = new List<CommandSequence>();

    /// <summary>
    /// 是否存在命令序列.
    /// </summary>
    /// <param name="seqName"></param>
    /// <returns></returns>
    public bool has(string seqName)
    {
        return findSequence(seqName) != null;
    }

    /// <summary>
    /// 当前是否再播放.
    /// </summary>
    public bool isPlaying
    {
        get { return isPlaying_; }
    }

    /// <summary>
    /// 播放一个命令序列.
    /// </summary>
    /// <param name="seqName"></param>
    public void play(string seqName, System.Action onComplete)
    {
        if (isPlaying_)
        {
            Debug.LogError(string.Format("Can not play sequence {0}:AnimationComposer is playing!", seqName), this);
        }

        var seq = findSequence(seqName);
        if (seq == null)
        {
            Debug.LogError(string.Format("Can not play sequence {0}:Can not find sequence!", seqName), this);
        }

        StartCoroutine(playSequence(seq, onComplete));
    }

    bool isPlaying_;
    Dictionary<GameObject, int> childAnimating_ = new Dictionary<GameObject, int>();

    CommandSequence findSequence(string seqName)
    {
        for (int i = 0; i < sequences.Count; i++)
        {
            var seq = sequences[i];
            if (seq.name == seqName)
                return seq;
        }

        return null;
    }

    IEnumerator playSequence(CommandSequence seq, System.Action onComplete)
    {
        isPlaying_ = true;
        foreach (var cmd in seq.commands)
        {
            if (cmd.type == CommandType.Deactivate)
            {
                cmd.target.SetActive(false);
            }
            else if (cmd.type == CommandType.PlayAnimation)
            {
                playAnimation(cmd.target, cmd.animName, cmd.animTime, cmd.reversePlay);
            }
            else if (cmd.type == CommandType.DeactivateChildren)
            {
                deactivateChildren(cmd.target);
            }
            else if (cmd.type == CommandType.PlayChildAnimation)
            {
                // 启动一个协程处理，不会造成阻塞.
                StartCoroutine(playChildAnimation(cmd.target, cmd.animName, cmd.animTime, cmd.time, cmd.reverse, cmd.reversePlay));
            }
            else if (cmd.type == CommandType.Wait)
            {
                yield return new WaitForSeconds(cmd.time);
            }
            else if (cmd.type == CommandType.WaitAnimation)
            {
                while (isAnimating(cmd.target))
                    yield return null;
            }
            else if (cmd.type == CommandType.WaitChildAnimation)
            {
                var parent = cmd.target;
                while (isChildAnimating(cmd.target))
                    yield return null;
            }
        }

        isPlaying_ = false;
        if (onComplete != null)
            onComplete();
    }

    /// <summary>
    /// 判断动画是否为Tween动画.
    /// </summary>
    /// <param name="animName"></param>
    /// <returns></returns>
    public static bool isTweenAnim(string animName)
    {
        return animName == "_fade" || animName == "_zoom";
    }

    /// <summary>
    /// 播放DOTween动画.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="animName"></param>
    /// <param name="animTime"></param>
    /// <param name="reversePlay"></param>
    void playTweenAnimation(GameObject go, string animName, float animTime, bool reversePlay)
    {
        if (animName == "_fade")
        {
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (!canvasGroup)
                canvasGroup = go.AddComponent<CanvasGroup>();
            if (animTime > 0)
            {
                canvasGroup.alpha = reversePlay ? 1 : 0;
                canvasGroup.DOFade(reversePlay ? 0 : 1, animTime);
            }
            else
                canvasGroup.alpha = reversePlay ? 0 : 1;
        }
        else if (animName == "_zoom")
        {
            if (animTime > 0)
            {
                go.transform.localScale = reversePlay ? Vector3.one : Vector3.zero;
                go.transform.DOScale(reversePlay ? 0 : 1, animTime);
            }
            else
                go.transform.localScale = reversePlay ? Vector3.zero : Vector3.one;
        }
        else
            Debug.LogError("Invalid tween animation name " + animName);
    }

    /// <summary>
    /// 判断对象是否正在播放Tween动画.
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    bool isTweening(GameObject go)
    {
        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup && DOTween.IsTweening(canvasGroup))
            return true;
        if (DOTween.IsTweening(go.transform))
            return true;
        return false;
    }

    /// <summary>
    /// 判断一个对象当前是否在播放动画.
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    bool isAnimating(GameObject go)
    {
        if (isTweening(go))
            return true;
        var ac = go.GetComponent<AnimationComposer>();
        if (ac != null && ac != this && ac.isPlaying)
            return true;
        var animation = go.GetComponent<Animation>();
        if (animation != null && animation.isPlaying)
            return true;
        return false;
    }

    /// <summary>
    /// 播放指定对象动画.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="animName"></param>
    /// <param name="animTime"></param>
    /// <param name="reversePlay"></param>
    void playAnimation(GameObject go, string animName, float animTime, bool reversePlay)
    {
        go.SetActive(true);
        if (isTweenAnim(animName))
        {
            // 播放Tween动画.
            playTweenAnimation(go, animName, animTime, reversePlay);
            return;
        }

        AnimationComposer ac = go.GetComponent<AnimationComposer>();
        if (ac != null && ac != this)
        {
            // 播放AnimationComposer sequence.
            ac.play(animName, null);
            return;
        }

        // 播放Animation.
        var animation = go.GetComponent<Animation>();
        if (animation == null)
        {
            Debug.LogError("No Animation component on target game object.", go);
        }

        var animationState = animation[animName];
        if (animationState == null)
        {
            Debug.LogError("Failed to find animation " + animName, go);
        }

        animationState.speed = reversePlay ? -1 : 1;
        animationState.time = reversePlay ? animationState.length : 0;
        animation.Play(animName);
    }

    void deactivateChildren(GameObject parent)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            child.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 标识一个对象的子对象正在播放动画
    /// </summary>
    /// <param name="go"></param>
    void addChildAnimatingObject(GameObject go)
    {
        if (!childAnimating_.ContainsKey(go))
            childAnimating_.Add(go, 0);
        var refCnt = childAnimating_[go];
        refCnt++;
        childAnimating_[go] = refCnt;
    }

    /// <summary>
    /// 移除一个子对象动画.
    /// </summary>
    /// <param name="go"></param>
    void removeChildAnimatingObject(GameObject go)
    {
        var refCnt = childAnimating_[go];
        refCnt--;
        if (refCnt <= 0)
            childAnimating_.Remove(go);
        else
            childAnimating_[go] = refCnt;
    }

    /// <summary>
    /// 检测对象是否在播放子对象动画.
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    bool isChildAnimating(GameObject go)
    {
        if (childAnimating_.ContainsKey(go))
            return true;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i);
            if (isAnimating(child.gameObject))
                return true;
        }

        return false;
    }

    IEnumerator playChildAnimation(GameObject parent, string animName, float animTime, float time, bool reverse, bool reversePlay)
    {
        addChildAnimatingObject(parent);
        var parentTrans = parent.transform;
        int start = reverse ? parentTrans.childCount - 1 : 0;
        int end = reverse ? -1 : parentTrans.childCount;
        int step = reverse ? -1 : 1;
        for (int i = start; i != end; i += step)
        {
            var child = parentTrans.GetChild(i);
            playAnimation(child.gameObject, animName, animTime, reversePlay);
            if (time > 0)
                yield return new WaitForSeconds(time);
        }

        removeChildAnimatingObject(parent);
    }
}