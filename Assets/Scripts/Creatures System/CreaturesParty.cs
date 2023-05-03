using System.Collections;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

public class CreaturesParty : MonoBehaviour
{
    public event Action onUpdated;
    public List<Creature> creatures;
    CreaturesBox creaturesBox;
    public List<Creature> Creatures
    {
        get
        {
            return creatures;
        }
        set
        {
            creatures = value;
            onUpdated?.Invoke();
        }
    }

    public void Start()
    {
        creaturesBox = CreaturesBox.GetPlayerBox();
        foreach (var creature in Creatures)
        {
            creature.Init();
        }
    }

    public Creature GetHealthyCreature()
    {
        return Creatures.Where(x => x.HP > 0).FirstOrDefault();
    }
    public void AddCreature(Creature newCreature)
    {
        if (Creatures.Count < 6)
        {
            Creatures.Add(newCreature);
            onUpdated?.Invoke();
        }
        else
        {
            // TODO: Add the to the PC
            creaturesBox.Add(newCreature);
        }
    }
    public static CreaturesParty GetPlayerParty()
    {
        return FindObjectOfType<Player>().GetComponent<CreaturesParty>();
    }
    public bool CheckForEvolutions()
    {
        return creatures.Any(p => p.CheckForEvolution() != null);
    }
    public IEnumerator RunEvolutions()
    {
        foreach (var creature in creatures)
        {
            var evolution = creature.CheckForEvolution();
            if (evolution != null)
            {
                yield return EvolutionManager.instance.Evolve(creature, evolution);
            }
        }
        onUpdated?.Invoke();
    }
    public void PartyUpdated()
    {
        onUpdated?.Invoke();
    }
    public string GetPartyDiscordStatus()
    {
        string[] names = creatures.Select(c => c._base.name).ToArray();
        int[] lvls = creatures.Select(c => c.level).ToArray();
        string res = "";
        for (int i = 0; i < creatures.Count; i++)
        {
            string text = $"{(names[i] != names[0] ? ", " : "")}{names[i]} {(creatures[i].GetName() == names[i] ? "" : ("\"" + creatures[i].GetName() + "\""))} lvl {lvls[i]}";
            res += $"{text}";
        }
        return res;
    }
}