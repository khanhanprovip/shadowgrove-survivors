using System;
using UnityEngine;
namespace RGame.RoguelikeKit
{
    [Serializable]
    public sealed class BossDeckState
    {
        public int[] cards;
        public int cursor;
        public int last = -1;
    }
    // Shuffle bag: every type appears once before a new bag is shuffled.
    public sealed class BossEncounterDeck
    {
        int[] cards = Array.Empty<int>();
        int cursor, last = -1;
        public int Peek()
        {
            if(cursor>=cards.Length)
            {
                cards=new[]{0,1,2,3,4};cursor=0;
                for(int i=4;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);(cards[i],cards[j])=(cards[j],cards[i]);}
                if(cards[0]==last){int j=UnityEngine.Random.Range(1,5);(cards[0],cards[j])=(cards[j],cards[0]);}
            }
            return cards[cursor];
        }
        public int Draw(){int kind=Peek();cursor++;last=kind;return kind;}
        public void Reset(){cards=Array.Empty<int>();cursor=0;last=-1;}
        public BossDeckState Capture()=>new BossDeckState{cards=(int[])cards.Clone(),cursor=cursor,last=last};
        public bool Restore(BossDeckState state)
        {
            if(state==null||state.cards==null||state.cursor<0||state.cursor>state.cards.Length||state.last < -1||state.last>4)return false;
            if(state.cards.Length!=0&&state.cards.Length!=5)return false;
            if(state.cards.Length==0&&(state.cursor!=0||state.last!=-1))return false;
            if(state.cards.Length==5&&state.cursor==0&&state.last>=0&&state.cards[0]==state.last)return false;
            int mask=0;
            foreach(int kind in state.cards){if(kind<0||kind>4||(mask&(1<<kind))!=0)return false;mask|=1<<kind;}
            if(state.cursor>0&&state.cards[state.cursor-1]!=state.last)return false;
            cards=(int[])state.cards.Clone();cursor=state.cursor;last=state.last;return true;
        }
    }
}
