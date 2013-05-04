using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDiff
{
   public class DiffOperation
    {
       int[] lineX;
       int[] lineY;

       //X-souradnice cesty k nejvzdalenejsimu bodu po diagonale smerem dopredu
       int[] diagForward;
       int forwardDiagMax;
       //X-souradnice cesty k nejvzdalenejsimu bodu po diagonale pri zpetnem prohledavani
       int[] diagBackward;
       int backwardDiagMax;
       
       private static int currentMaxEquivalence = 1;
       
       /// <summary>
       /// pocet radku vlozenych nebo smazanych, vyuziva se pri porovnavani
       /// </summary>
       int edits;
       
       private DiffResult res;
       DataContainer origData;
       DataContainer newData;

      public DiffResult getDiff(DiffRequest req){
          res = new DiffResult(req.hash, req);
          Dictionary<String, int> dic = new Dictionary<String, int>(req.original.Length + req.edited.Length);
          origData = new DataContainer(req.original, dic);
          newData = new DataContainer(req.edited, dic);
          //odstraneni radku, ktere jsou v jednom a nejsou v druhem souboru
          Remove_InsAdd();

          //vlozeni indexu os X a Y
          lineX = origData.notRemoved;
          lineY = newData.notRemoved;

          diagForward = new int[origData.notRemovedCount + newData.notRemovedCount + 3];
          forwardDiagMax = newData.notRemovedCount + 1;
          diagBackward = new int[origData.notRemovedCount + newData.notRemovedCount + 3];
          backwardDiagMax = newData.notRemovedCount + 1;

          compare(0, origData.notRemovedCount, 0, newData.notRemovedCount);
          
          return buildResult(origData.changedFlags, origData.originalIndexes.Length,
              newData.changedFlags, newData.originalIndexes.Length);
      }

       /// <summary>
       /// na zaklade priznaku zmenenych radku v originalnim a novem souboru sestavi seznam zmen
       /// </summary>
       /// <param name="originalFlags"></param>
       /// <param name="originalSize"></param>
       /// <param name="newFlags"></param>
       /// <param name="newSize"></param>
       /// <returns></returns>
      private DiffResult buildResult(bool[] originalFlags, int originalSize, bool[] newFlags, int newSize)
      {
          int pomOrig = 0;
          int pomNew = 0;
          while (pomOrig < originalSize || pomNew < newSize)
          {
              if (originalFlags[pomOrig+1] || newFlags[pomNew+1])
              {
                  int firstLineOld = pomOrig, firstLineNew = pomNew;

                  //prochazime zmenene radky v kazdem souboru, dokud nenarazime na nezmeneny
                  while (originalFlags[pomOrig+1]) pomOrig++;
                  while (newFlags[pomNew+1]) pomNew++;

                  //zaznamename posloupnost zmeny na vystup
                  if (pomOrig-firstLineOld == 0)
                  {//add
                      res.addLine(new ResultLine(ResultLine.ChangeType.ADDITION, firstLineOld, pomOrig, firstLineNew+1, pomNew));
                  }else
                      if (pomNew-firstLineNew == 0)
                      {//remove
                          res.addLine(new ResultLine(ResultLine.ChangeType.REMOVAL, firstLineOld+1, pomOrig, firstLineNew, pomNew));
                      }
                      else
                      {//change
                          res.addLine(new ResultLine(ResultLine.ChangeType.CHANGE, firstLineOld+1, pomOrig, firstLineNew+1, pomNew));
                      }
              }
              pomOrig++; pomNew++;
          }
          return res;
      }

      private void compare(int xOff, int xLimit, int yOff, int yLimit)
      {
          //pocatecni shodne radky - jdeme po diagonale
          while (xOff < xLimit && yOff < yLimit && lineX[xOff] == lineY[yOff])
          {
              xOff++; 
              yOff++;
          }

          //shodne radky od konce - jdeme po diagonale z praveho dolniho rohu
          while (xLimit > xOff && yLimit > yOff && lineX[xLimit - 1] == lineY[yLimit-1])
          {
              xLimit--;
              yLimit--;
          }

          if (xOff == xLimit)//po X jsme uz na konci, dojdeme Y (newData)
          {
              while (yOff < yLimit)
              {
                  newData.changedFlags[newData.originalIndexes[yOff] + 1] = true;
                  yOff++;
              }
          }
          else if (yOff == yLimit)//po Y uz jsme na konci, dojdeme X (origData)
          {
              while (xOff < xLimit)
              {
                  origData.changedFlags[origData.originalIndexes[xOff] + 1] = true;
                  xOff++;
              }
          }//nedosli sme na konec X ani Y osy
          else
          {
              //bod kde se soubory shoduji
              int d = FindDiagonal(xOff, xLimit, yOff, yLimit);
              int forward = diagForward[forwardDiagMax + d];
              int backward = diagBackward[backwardDiagMax + d];

              //rozdeleni na podproblemy dle bodu d
              compare(xOff, backward, yOff, backward - d);
              compare(backward, xLimit, backward - d, yLimit);
          }
      }

       /// <summary>
       /// hledame  diagonalu na ktere lezi stredni bod nejkratsi cesty (skriptu) specifickych casti dvou souboru pomoci prohledavani do sirky
       /// </summary>
       /// <param name="xOff"></param>
       /// <param name="xLimit"></param>
       /// <param name="yOff"></param>
       /// <param name="yLimit"></param>
       /// <returns></returns>
      private int FindDiagonal(int xOff, int xLimit, int yOff, int yLimit)
      {
          //omezeni pro prohledavani shora dolu
          int fmin = xOff - yOff, fmax = fmin;
          //omezeni pro prohledavani zespoda nahoru
          int bmin = xLimit - yLimit, bmax = bmin;	
    
          //pokud je pravy dolni roh na liche diagonale vzhledem k levemu hornimu
          bool odd = ((xOff - yOff) - (xLimit - yLimit) & 1) != 0;

          diagForward[forwardDiagMax + xOff - yOff] = xOff;
          diagBackward[backwardDiagMax + xLimit - yLimit] = xLimit;

          for (int c = 1;; c++)
          {
	        int d;			
	        //na kazde diagonale se posuneme o jeden krok
            if (fmin > xOff - yLimit)
                diagForward[forwardDiagMax + --fmin - 1] = -1;
	        else
	            fmin++;
            if (fmax < xLimit - yOff)
                diagForward[forwardDiagMax + ++fmax + 1] = -1;
	        else
	            fmax--;
	        for (d = fmax; d >= fmin; d -= 2)//udrzujeme lichost/sudost diagonaly
	        {
                int x, y;
                //mozne smery
                int tlo = diagForward[forwardDiagMax + d - 1], thi = diagForward[forwardDiagMax + d + 1];
	            if (tlo >= thi) x = tlo + 1;
	            else x = thi;
	        
	            y = x - d;
                while (x < xLimit && y < yLimit && lineX[x] == lineY[y])//jdu po diagonale dokud mohu
                {
    	            x++; y++;
	            }
                diagForward[forwardDiagMax + d] = x;
                if (odd && bmin <= d && d <= bmax && diagBackward[backwardDiagMax + d] <= diagForward[forwardDiagMax + d])
	                return d;
            }

	        //posun v prohledavani z praveho dolniho rohu
            if (bmin > xOff - yLimit)
                diagBackward[backwardDiagMax + --bmin - 1] = int.MaxValue;
	        else
	            ++bmin;
            if (bmax < xLimit - yOff)
                diagBackward[backwardDiagMax + ++bmax + 1] = int.MaxValue;
            else
                bmax--;
            for (d = bmax; d >= bmin; d -= 2)//udrzujeme lichost/sudost diagonaly
	        {
                int x, y;
                //mozne smery
                int tlo = diagBackward[backwardDiagMax + d - 1], thi = diagBackward[backwardDiagMax + d + 1];
	            if (tlo < thi)  x = tlo;
	            else x = thi - 1;
	            
                y = x - d;
                while (x > xOff && y > yOff && lineX[x - 1] == lineY[y - 1])
                {//postup po diagonale
	                x--; y--;
	            }
                diagBackward[backwardDiagMax + d] = x;
                if (!odd && fmin <= d && d <= fmax && diagBackward[backwardDiagMax + d] <= diagForward[forwardDiagMax + d])
	               return d;
	         }
        }
      }

      private void Remove_InsAdd()
      {
          origData.RemoveNotMatched(newData);
          newData.RemoveNotMatched(origData);
      }

      class DataContainer
      {
          /// <summary>
          /// indexy ekvivalence - vsechny shodne radky maji shodne cislo
          /// </summary>
          int[] lineEquivalence;

          /// <summary>
          /// puvodni indexy radku pred filtrovanim
          /// </summary>
          public int[] originalIndexes;

          /// <summary>
          /// indexy neodebranych radku
          /// </summary>
          public int[] notRemoved;

          /// <summary>
          /// pocet neodebranych radku
          /// </summary>
          public int notRemovedCount;

          /// <summary>
          /// flag je tru na indexu, který určuje index radku, pokud byl radek zmenen
          /// </summary>
          public bool[] changedFlags;

          public DataContainer(String[] data, Dictionary<String, int> d)
          {
              lineEquivalence = new int[data.Length];
              originalIndexes = new int[data.Length];
              notRemoved = new int[data.Length];

              for (int i = 0; i < data.Length; ++i)
              {
                  int inDict = d.TryGetValue(data[i], out inDict) ? inDict : -1;
                  if (inDict == -1)
                  {
                      d.Add(data[i], lineEquivalence[i] = currentMaxEquivalence);
                      currentMaxEquivalence++;
                  }
                  else
                  {
                      lineEquivalence[i] = inDict;
                  }
              }
          }

          /// <summary>
          /// Odstrani radky, ktere nejsou v souboru specifikovaném parametrem
          /// </summary>
          /// <param name="container"></param>
          internal void RemoveNotMatched(DataContainer container)
          {
              changedFlags = new bool[this.originalIndexes.Length + 2];  

              byte[] toBeRemoved = Removable(container.EquivalentArray());
              RemoveLines(toBeRemoved);
          }

          /// <summary>
          /// Odstrani radky oznacene k odstraneni
          /// </summary>
          /// <param name="toBeRemoved"></param>
          private void RemoveLines(byte[] toBeRemoved)
          {
              int end = this.originalIndexes.Length;
              int count = 0;
              for (int i = 0; i < end; ++i)
                  if (toBeRemoved[i] == 0)
                  {
                      notRemoved[count] = lineEquivalence[i];
                      originalIndexes[count] = i;
                      count++;
                  }
                  else
                  {
                      changedFlags[i + 1] = true;
                  }
              notRemovedCount = count;
          }

          /// <summary>
          /// oznacime jednotlive radky, zda mohou byt odstraneny(1), nebo ne(0)
          /// </summary>
          /// <param name="equivCounts"></param>
          /// <returns></returns>
          private byte[] Removable(int[] equivCounts)
          {
              int end = this.originalIndexes.Length;
              byte[] toBeRemoved = new byte[end];
              int[] le = this.lineEquivalence;
              int eq;

              for (int i = 0; i < end; i++)
              {
                  if (le[i] != 0)
                  {
                      eq = equivCounts[le[i]];
                      if (eq == 0) toBeRemoved[i] = 1;
                  }
              }
              return toBeRemoved;
          }

          /// <summary>
          /// spocte shodne radky v souboru
          /// </summary>
          /// <returns></returns>
          private int[] EquivalentArray()
          {
              int[] ea = new int[currentMaxEquivalence];
              for (int i = 0; i < this.originalIndexes.Length; ++i)
                  ea[lineEquivalence[i]]++;
              return ea;
          }
      }
    }
}
