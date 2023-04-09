using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class NPCController : MonoBehaviour, Interactable
{
    public Dialog dialog;
    public QuestBase questToStart, questToComplete;
    public List<Sprite> sprites;
    public List<Vector2> movementPattern;
    public float timeBetweenPatterns;
    NPCState state;
    float idleTimer = 0f;
    public Character character;
    ItemGiver itemGiver;
    CreatureGiver creatureGiver;
    SpriteAnimator spriteAnimator;

    Quest activeQuest;
    void Start()
    {
        spriteAnimator = new SpriteAnimator(sprites, GetComponentInChildren<SpriteRenderer>());
        character = GetComponent<Character>();
        itemGiver = GetComponent<ItemGiver>();
        spriteAnimator.Start();
        creatureGiver = GetComponent<CreatureGiver>();
    }
    void Update()
    {
        if (state == NPCState.Idle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer > 2f)
            {
                idleTimer = 0f;
                if (movementPattern.Count > 0)
                {
                    spriteAnimator.HandleUpdate();
                    var pattern = movementPattern[Random.Range(0, movementPattern.Count)];
                    state = NPCState.Walking;
                }
            }
        }

    }
    public IEnumerator Interact(Transform initiator = null)
    {
        idleTimer = 0f;
        state = NPCState.Idle;
        if (questToComplete != null){
            var quest = new Quest(questToComplete);
            yield return quest.CompleteQuest();
            questToComplete = null;
        }
        if (itemGiver != null && itemGiver.CanBeGiven())
        {
            yield return itemGiver.GiveItem(initiator.GetComponent<Player>());
        } else if (creatureGiver != null && creatureGiver.CanBeGiven()){
            yield return creatureGiver.GiveCreature(initiator.GetComponent<Player>());
        }
        else if (questToStart != null)
        {
            activeQuest = new Quest(questToStart);
            yield return activeQuest.StartQuest();
            questToStart = null;

            if (activeQuest.CanBeCompleted()){
                yield return activeQuest.CanBeCompleted();
                activeQuest = null;
            }
        }
        else if (activeQuest != null)
        {
            if (activeQuest.CanBeCompleted())
            {
                yield return activeQuest.CanBeCompleted();
                activeQuest = null;
            }
            else
            {
                yield return DialogManager.instance.ShowDialog(activeQuest.Base.InProgressDialog);
            }
        }
        else
        {
            yield return (DialogManager.instance.ShowDialog(dialog));

        }
    }
}
public enum NPCState
{
    Idle, Walking
}
