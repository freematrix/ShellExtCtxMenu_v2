/********************************** Module Header **********************************\
Module Name:  FileContextMenuExt.cs
Project:      CSShellExtContextMenuHandler
Copyright (c) Microsoft Corporation.

The FileContextMenuExt.cs file defines a context menu handler by implementing the 
IShellExtInit and IContextMenu interfaces.

This source is subject to the Microsoft Public License.
See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
All other rights reserved.

THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***********************************************************************************/

#region Using directives
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Xml;
#endregion


namespace CSShellExtContextMenuHandler
{
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("B1F1405D-94A1-4692-B72F-FC8CAF8B8700"), ComVisible(true)]
    public class FileContextMenuExt : IShellExtInit, IContextMenu
    {
        // The name of the selected file. (guradare i vari punti xxxxxxx per gestire il caso di un solo file)
        private StringCollection selectedFiles;

        //(S = Yes, N = No)
		private char SN_FileArg; //se 'S' allora ho come argomenti almeno un file, se no 'N' 
        private char SN_FolderArg; //se 'S' allora ho come argomenti almeno un folder, se no 'N'

        private string verbCanonicalName = "shShell";
        private string verbHelpText = "shShell";
        private string currentDir = ""; //directory corrente della dll (dll curr directory)
        private uint IDM_DISPLAY = 0;
        private uint IDM_CMDFIRST;  //è idCmdFirst passato in QuryContextMenu
        private string listaEstensioniFile = ""; //elencate le estensioni dei file selezionati (es:   exe,PDF,xls,) (list extensions)
        private string sSizeFiles = ""; //elencate le dimensioni dei file selezionati in MByte (es: 50,2,4,) (size files)
        private List<ItemXML> listaItem = new List<ItemXML>(); //lista pathProg e opzionoi passati via xml
		
		
        #region my personal function
        
        /// <summary>
        /// /questa è il metodo che invoco alla fine di tutto, in selectedFiles ho file scelti
        /// </summary>
        /// <param name="sProgramma"></param>
        /// <param name="sOpzione"></param>
        void avviaProgramma(ItemXML item)
        {            
            if (!System.IO.File.Exists(item.sPathProg))
            {
                System.Windows.Forms.MessageBox.Show("attenzione il programma\r\n" + item.sPathProg + "\r\nnon esiste");
                return;
            }
            
            string sArgomenti = "";
            foreach (string s in selectedFiles)
            {
                if (System.IO.File.Exists(s) || System.IO.Directory.Exists(s))
                {
                    sArgomenti += item.sPrefixItem + s + item.sSuffixItem + item.sSeparatore;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("attenzione il file o directory\r\n" + s + "\r\nnon esiste");
                    return;
                }

            }

            

            //tolgo l'ultimo carattere a sArgomenti
            if (!sArgomenti.Equals(""))
            {
                sArgomenti = sArgomenti.Substring(0, sArgomenti.Length - 1);
            }



            if (("\"" + item.sPathProg + "\" " + sArgomenti.Trim()).Length >= 2050)
            {
                //se il comando totale è più lungo di 2080 (per sicurezza ho messo 2050) caratteri process.start 
                //genera una Win32Exception, il problema è aggirato creando un bat nella directory della dll
                System.IO.File.WriteAllText(currentDir + "\\comando.bat", "\"" + item.sPathProg + "\" " + sArgomenti.Trim());

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "cmd.exe";
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.Arguments = "/c \"" + currentDir + "\\comando.bat\"";

                System.Diagnostics.Process.Start(startInfo);
            }
            else
                System.Diagnostics.Process.Start(item.sPathProg, sArgomenti.Trim());

        }

        /// <summary>
        /// mi ritorna la directory corrente contenente la dll
        /// </summary>
        /// <returns></returns>
        string getCurrentDir()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return System.IO.Path.GetDirectoryName(path);
        }

        /// <summary>
        /// creo un item e lo aggiungo al menu. Un item può essere un separatore (item con nome "separatore") o un normale item con
        /// path valorizzata
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="reader"></param>
        /// <param name="IdElemento"></param>
        /// <param name="posizione"></param>
        public void creaItem(IntPtr hMenu, XmlTextReader reader, uint IdElemento, uint posizione)
        {
            MENUITEMINFO mii = new MENUITEMINFO();

            string sNome = "";
            string sImg = "";
            string sPathProg = "";
            string sSeparatore = "";

            //ciclo su tutti gli attributi del nodo attuale e valorizzo eventualmente gli attributi
            settaAttributi(reader, ref sNome, ref sImg, ref sPathProg, ref sSeparatore);

            if (sNome.ToLower().Equals("separatore"))
            {
                mii.cbSize = (uint)Marshal.SizeOf(mii);
                mii.fMask = MIIM.MIIM_TYPE;
                mii.fType = MFT.MFT_SEPARATOR;

                //devo aggiungere per forza un ogg ItemXML, ALTRIMENTI DOVREI DECREMENTARE IdElemento 
                listaItem.Add(new ItemXML("", ""));
            }
            else //è un normale item
            {
                mii.cbSize = (uint)Marshal.SizeOf(mii);
                if (!sImg.Equals(""))
                    mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_TYPE | MIIM.MIIM_STATE | MIIM.MIIM_CHECKMARKS;
                else
                    mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_TYPE | MIIM.MIIM_STATE;

                mii.wID = IdElemento;
                mii.fType = MFT.MFT_STRING;
                mii.dwTypeData = sNome;
                mii.fState = MFS.MFS_ENABLED;

                if (!sImg.Equals(""))
                {
                    IntPtr aa = LoadImage(IntPtr.Zero, currentDir + "\\" + sImg, 0, 15, 15, 0x00000010 | 0x00008000);
                    mii.hbmpUnchecked = aa;
                    mii.hbmpChecked = aa;
                }

                //mi ricordo il sPathProg e sOpzione dell'item in posizione (IdElemento - IDM_CMDFIRST - IDM_DISPLAY)
                //e lo aggiungo alla lista
                listaItem.Add(new ItemXML(sPathProg, sSeparatore));
            }
            NativeMethods.InsertMenuItem(hMenu, posizione, true, ref mii);
        }

        public void creaMenu(IntPtr hMenu, XmlTextReader reader, ref uint IdElemento, uint posizione)
        {
            IntPtr hmnuPopup = CreatePopupMenu();

            string sNome = "";
            string sImg = "";
            string sPathProg = "";
            string sSeparatore = "";

            settaAttributi(reader, ref sNome, ref sImg, ref sPathProg, ref sSeparatore);

            //ciclo sui figli di dell'attuale nodo menu per vedere se devo aggiungere ancora altri menu o item
            PopulateMenu(reader, hmnuPopup, ref IdElemento);

            MENUITEMINFO mii = new MENUITEMINFO();
            mii.cbSize = (uint)Marshal.SizeOf(mii);

            if (!sImg.Equals(""))
                mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_TYPE | MIIM.MIIM_STATE | MIIM.MIIM_CHECKMARKS | MIIM.MIIM_SUBMENU;
            else
                mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_TYPE | MIIM.MIIM_STATE | MIIM.MIIM_SUBMENU;

            mii.hSubMenu = hmnuPopup;
            mii.fType = MFT.MFT_STRING;
            mii.dwTypeData = sNome;
            mii.fState = MFS.MFS_ENABLED;

            if (!sImg.Equals(""))
            {
                IntPtr hImg = LoadImage(IntPtr.Zero, currentDir + "\\" + sImg, 0, 15, 15, 0x00000010 | 0x00008000);
                mii.hbmpUnchecked = hImg;
                mii.hbmpChecked = hImg;
            }

            NativeMethods.InsertMenuItem(hMenu, posizione, true, ref mii);
        }

        /// <summary>
        /// popolo il menu hMenu con gli item e i sottomenu descritti nel file xml
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="hMenu"></param>
        /// <param name="IdElemento"></param>
        void PopulateMenu(XmlTextReader reader, IntPtr hMenu, ref uint IdElemento)
        {
            int profondita = reader.Depth;
            uint posizione = 0;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    //if (reader.GetAttribute("visible") != null && reader.GetAttribute("visible").Equals("false"))
                    if (saltaNodo(reader))
                    {
                        moveToEndElement(reader);
                    }
                    else if (reader.Name.ToLower().Equals("item"))
                    {
                        creaItem(hMenu, reader, IdElemento++, posizione++);
                    }
                    else if (reader.Name.ToLower().Equals("menu"))
                    {
                        creaMenu(hMenu, reader, ref IdElemento, posizione++);
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == profondita)
                    break;
            }
        }

        /// <summary>
        /// mi posiziono sull'end element del nodo corrente
        /// </summary>
        /// <param name="reader"></param>
        private void moveToEndElement(XmlTextReader reader)
        {
            int profondita = reader.Depth;
            while (reader.Read())
            {
                if (reader.Depth <= profondita)
                    break;
            }
        }

        /// <summary>
        /// sul nodo corrente ciclo su tutti gli attributi e valorizzo le variabili passate come paramtri
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="sNome"></param>
        /// <param name="sImg"></param>
        /// <param name="sPathProg"></param>
        /// <param name="sOpzione"></param>
        public void settaAttributi(XmlTextReader reader, ref string sNome, ref string sImg, ref string sPathProg, ref string sSeparatore)
        {
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "nome":
                            sNome = reader.Value;
                            break;
                        case "img":
                            sImg = reader.Value;
                            break;
                        case "path":
                            sPathProg = reader.Value;
                            break;
                        case "separatore":
                            sSeparatore = reader.Value;
                            break;
                    }
                }
                // Move the reader back to the element node.
                reader.MoveToElement();
            }
        }

        public bool saltaNodo(XmlTextReader reader)
        {
            string visible = "";
            
            // ---------  Controllo sul campo visible - se a false esco subito e non faccio altri cotrolli -------/
            if (reader.GetAttribute("visible") != null)
                visible = reader.GetAttribute("visible");

            if (visible.Equals("false"))
                return true;

            //di default il nodo non verrà saltato
            return false; 
        }

        //ritorno la lista delle estensioni dei file selezionati nella forma  pdf,exe,ppt, (con la virgola finale)
        public void setListaEstensioniFile()
        {
            foreach (string s in selectedFiles)
            {
                if (System.IO.File.Exists(s))
                    listaEstensioniFile += System.IO.Path.GetExtension(s) + ",";
            }
        }

        //setto le variabili
        //SN_FileArg;      se 'S' allora ho come argomenti almeno un file, se non 'N'
        //SN_FolderArg;    se 'S' allora ho come argomenti almeno un folder, se non 'N'
        public void settaSN_File_Folder_Arg()
        {
            SN_FileArg = 'N';
            SN_FolderArg = 'N';

            foreach (string s in selectedFiles)
            {
                if (System.IO.File.Exists(s))
                    SN_FileArg = 'S';
                else if (System.IO.Directory.Exists(s))
                    SN_FolderArg = 'S';
            }
        }

        //setto la stringa sSizeFiles che elenca le dimensioni dei file selezionati in MByte (es: 50,2,4,)
        public void creaStringaDimensioniFiles()
        {
            foreach (string s in selectedFiles)
            {
                if (System.IO.File.Exists(s))
                    sSizeFiles += (new System.IO.FileInfo(s).Length / 1000000) + ",";
            }
        }

        
        #endregion

        #region Shell Extension Registration

        [ComRegisterFunction()]
        public static void Register(Type t)
        {
            try
            {
                //menu on files and folders
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, "AllFilesystemObjects", "CSShellExtContextMenuHandler.FileContextMenuExt Class");

                //ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".cs", "CSShellExtContextMenuHandler.FileContextMenuExt Class");
				//ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, "DesktopBackground", "CSShellExtContextMenuHandler.FileContextMenuExt Class");
                //ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, "*", "CSShellExtContextMenuHandler.FileContextMenuExt Class");
                //ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, "Directory", "CSShellExtContextMenuHandler.FileContextMenuExt Class");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw;  // Re-throw the exception
            }
        }

        [ComUnregisterFunction()]
        public static void Unregister(Type t)
        {
            try
            {
                //ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".cs");
				
                //tolgo la chiave nel registro 
                //delete key on register
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, "AllFilesystemObjects");
				
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw;  // Re-throw the exception
            }
        }

        #endregion

        #region IShellExtInit Members
        /// <summary>
        /// Initialize the context menu handler.
        /// </summary>
        /// <param name="pidlFolder">
        /// A pointer to an ITEMIDLIST structure that uniquely identifies a folder.
        /// </param>
        /// <param name="pDataObj">
        /// A pointer to an IDataObject interface object that can be used to retrieve 
        /// the objects being acted upon.
        /// </param>
        /// <param name="hKeyProgID">
        /// The registry key for the file object or folder type.
        /// </param>
        public void Initialize(IntPtr pidlFolder, IntPtr pDataObj, IntPtr hKeyProgID)
        {
            if (pDataObj == IntPtr.Zero)
            {
                throw new ArgumentException();
            }

            FORMATETC fe = new FORMATETC();
            fe.cfFormat = (short)CLIPFORMAT.CF_HDROP;
            fe.ptd = IntPtr.Zero;
            fe.dwAspect = DVASPECT.DVASPECT_CONTENT;
            fe.lindex = -1;
            fe.tymed = TYMED.TYMED_HGLOBAL;
            STGMEDIUM stm = new STGMEDIUM();

            // The pDataObj pointer contains the objects being acted upon. In this 
            // example, we get an HDROP handle for enumerating the selected files 
            // and folders.
            IDataObject dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);
            dataObject.GetData(ref fe, out stm);

            try
            {
                
                // Get an HDROP handle.
                IntPtr hDrop = stm.unionmember;
                if (hDrop == IntPtr.Zero)
                {
                    throw new ArgumentException();
                }

                // Determine how many files are involved in this operation.
                uint nFiles = NativeMethods.DragQueryFile(hDrop, UInt32.MaxValue, null, 0);

                
				
				/*
                // This code sample displays the custom context menu item when only 
                // one file is selected. 
                if (nFiles == 1)
                {
                    // Get the path of the file.
                    StringBuilder fileName = new StringBuilder(260);
                    if (0 == NativeMethods.DragQueryFile(hDrop, 0, fileName,
                        fileName.Capacity))
                    {
                        Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                    }
                    this.selectedFile = fileName.ToString();
                }
                else
                {
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
				*/
                
				
				// [or]
                // provo a lanciare l'estensione anche senza avere file selezionati
                selectedFiles = new StringCollection();
                StringBuilder fileName = new StringBuilder(260);
                for (uint i = 0; i < nFiles; i++)
                {
                    // Get the next file name.
                    if (0 != NativeMethods.DragQueryFile(hDrop, i, fileName,
                        fileName.Capacity))
                    {
                        // Add the file name to the list.
                        selectedFiles.Add(fileName.ToString());
                    }
                }

                // [-or-]
                // Enumerate the selected files and folders.
                // elenco i file selezionati - commentato perchè provo a lanciare l'estensione anche se non ho file selezionati (non ci sono riuscito - prob devo adattare la chiamata RegisterShellExtContextMenuHandler passando come secondo argomento ... (non lo so) )
                /*
                if (nFiles > 0)
                {
                    selectedFiles = new StringCollection();
                    StringBuilder fileName = new StringBuilder(260);
                    for (uint i = 0; i < nFiles; i++)
                    {
                        // Get the next file name.
                        if (0 != NativeMethods.DragQueryFile(hDrop, i, fileName,
                            fileName.Capacity))
                        {
                            // Add the file name to the list.
                            selectedFiles.Add(fileName.ToString());
                        }
                    }
                
                    // If we did not find any files we can work with, throw 
                    // exception.
                    if (selectedFiles.Count == 0)
                    {
                        Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                    }
                }
                else
                {
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
                */
            }
            finally
            {
                NativeMethods.ReleaseStgMedium(ref stm);
            }
        }
        #endregion

        #region IContextMenu Members
	
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType,
           int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        static extern IntPtr CreatePopupMenu();
	
        /// <summary>
        /// Add commands to a shortcut menu.
        /// </summary>
        /// <param name="hMenu">A handle to the shortcut menu.</param>
        /// <param name="iMenu">
        /// The zero-based position at which to insert the first new menu item.
        /// </param>
        /// <param name="idCmdFirst">
        /// The minimum value that the handler can specify for a menu item ID.
        /// </param>
        /// <param name="idCmdLast">
        /// The maximum value that the handler can specify for a menu item ID.
        /// </param>
        /// <param name="uFlags">
        /// Optional flags that specify how the shortcut menu can be changed.
        /// </param>
        /// <returns>
        /// If successful, returns an HRESULT value that has its severity value set 
        /// to SEVERITY_SUCCESS and its code value set to the offset of the largest 
        /// command identifier that was assigned, plus one.
        /// </returns>
        public int QueryContextMenu(
            IntPtr hMenu,
            uint iMenu,
            uint idCmdFirst,
            uint idCmdLast,
            uint uFlags)
        {
            // If uFlags include CMF_DEFAULTONLY then we should not do anything.
            if (((uint)CMF.CMF_DEFAULTONLY & uFlags) != 0)
            {
                return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);
            }

            
			
            

			//ad ogni voce che posso cliccare associo un id che parte da idCmdFirst
            //ai separatori e ai menu che sono dei sottomenu non do l'id
            //questo id mi servirà poi nella funzione InvokeCommand per distinguere quale voce ho cliccato
            //
            //iMenu è la posizione dell'item nel menu
            //nel menu principale parto da iMenu e cresco di uno ad ogni voce che aggiungo
            //nei sottomenu parto zero e cresco per ogni item che aggiungo



            uint IdElemento = idCmdFirst + IDM_DISPLAY;
            uint posizione = iMenu;
            IDM_CMDFIRST = idCmdFirst;
            currentDir = getCurrentDir();

            try
            {
                setListaEstensioniFile();
                settaSN_File_Folder_Arg();
                creaStringaDimensioniFiles();

                XmlTextReader reader = new XmlTextReader(currentDir + "\\xmlSetting.xml");
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        //if (reader.GetAttribute("visible") != null && reader.GetAttribute("visible").Equals("false"))
                        if (saltaNodo(reader))
                        {
                            moveToEndElement(reader);
                        }
                        else if (reader.Name.ToLower().Equals("item"))
                        {
                            creaItem(hMenu, reader, IdElemento++, posizione++);
                        }
                        else if (reader.Name.ToLower().Equals("menu"))
                        {
                            creaMenu(hMenu, reader, ref IdElemento, posizione++);
                        }
                    }
                }
                reader.Close();
                //System.Windows.Forms.MessageBox.Show((IdElemento - (idCmdFirst + IDM_DISPLAY)).ToString());
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "\r\nhello");
            }

            


            //riorno il numero di voci che effettivmanete posso cliccare - cioè a cui è associata un azione 
            //non contare i separatori e i menu che hanno qualche figlio (cioè cha hanno dei sottomenu)

            //funziona correttamente anche se ritorno il numero di item aggiunti più i separatori, come faccio poi in questa dll
            return (int)(IdElemento - (idCmdFirst + IDM_DISPLAY));
			
			
			
			
			
			
			
			//original source code
			/*
            MENUITEMINFO mii = new MENUITEMINFO();
            mii.cbSize = (uint)Marshal.SizeOf(mii);
            mii.fMask = MIIM.MIIM_ID | MIIM.MIIM_TYPE | MIIM.MIIM_STATE;
            mii.wID = idCmdFirst + IDM_DISPLAY;
            mii.fType = MFT.MFT_STRING;
            mii.dwTypeData = menuText;
            mii.fState = MFS.MFS_ENABLED;
            if (!NativeMethods.InsertMenuItem(hMenu, iMenu, true, ref mii))
            {
                return Marshal.GetHRForLastWin32Error();
            }

            // Add a separator.
            MENUITEMINFO sep = new MENUITEMINFO();
            sep.cbSize = (uint)Marshal.SizeOf(sep);
            sep.fMask = MIIM.MIIM_TYPE;
            sep.fType = MFT.MFT_SEPARATOR;
            if (!NativeMethods.InsertMenuItem(hMenu, iMenu + 1, true, ref sep))
            {
                return Marshal.GetHRForLastWin32Error();
            }

            // Return an HRESULT value with the severity set to SEVERITY_SUCCESS. 
            // Set the code value to the offset of the largest command identifier 
            // that was assigned, plus one (1).
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0,IDM_DISPLAY + 1);
			*/
        }

        /// <summary>
        /// Carry out the command associated with a shortcut menu item.
        /// </summary>
        /// <param name="pici">
        /// A pointer to a CMINVOKECOMMANDINFO or CMINVOKECOMMANDINFOEX structure 
        /// containing information about the command. 
        /// </param>
        public void InvokeCommand(IntPtr pici)
        {
		    
            
			CMINVOKECOMMANDINFO ici = (CMINVOKECOMMANDINFO)
                                    Marshal.PtrToStructure(pici, typeof(CMINVOKECOMMANDINFO));

            int n = (int)ici.lpVerb;

            avviaProgramma(listaItem[n]);
			
			
			
			
		
			//original source code
			/*
            bool isUnicode = false;

            // Determine which structure is being passed in, CMINVOKECOMMANDINFO or 
            // CMINVOKECOMMANDINFOEX based on the cbSize member of lpcmi. Although 
            // the lpcmi parameter is declared in Shlobj.h as a CMINVOKECOMMANDINFO 
            // structure, in practice it often points to a CMINVOKECOMMANDINFOEX 
            // structure. This struct is an extended version of CMINVOKECOMMANDINFO 
            // and has additional members that allow Unicode strings to be passed.
            CMINVOKECOMMANDINFO ici = (CMINVOKECOMMANDINFO)Marshal.PtrToStructure(
                pici, typeof(CMINVOKECOMMANDINFO));
            CMINVOKECOMMANDINFOEX iciex = new CMINVOKECOMMANDINFOEX();
            if (ici.cbSize == Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX)))
            {
                if ((ici.fMask & CMIC.CMIC_MASK_UNICODE) != 0)
                {
                    isUnicode = true;
                    iciex = (CMINVOKECOMMANDINFOEX)Marshal.PtrToStructure(pici,
                        typeof(CMINVOKECOMMANDINFOEX));
                }
            }

            // Determines whether the command is identified by its offset or verb.
            // There are two ways to identify commands:
            // 
            //   1) The command's verb string 
            //   2) The command's identifier offset
            // 
            // If the high-order word of lpcmi->lpVerb (for the ANSI case) or 
            // lpcmi->lpVerbW (for the Unicode case) is nonzero, lpVerb or lpVerbW 
            // holds a verb string. If the high-order word is zero, the command 
            // offset is in the low-order word of lpcmi->lpVerb.

            // For the ANSI case, if the high-order word is not zero, the command's 
            // verb string is in lpcmi->lpVerb. 
            if (!isUnicode && NativeMethods.HighWord(ici.lpVerb.ToInt32()) != 0)
            {
                // Is the verb supported by this context menu extension?
                if (Marshal.PtrToStringAnsi(ici.lpVerb) == this.verb)
                {
                    OnVerbDisplayFileName(ici.hwnd);
                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }

            // For the Unicode case, if the high-order word is not zero, the 
            // command's verb string is in lpcmi->lpVerbW. 
            else if (isUnicode && NativeMethods.HighWord(iciex.lpVerbW.ToInt32()) != 0)
            {
                // Is the verb supported by this context menu extension?
                if (Marshal.PtrToStringUni(iciex.lpVerbW) == this.verb)
                {
                    OnVerbDisplayFileName(ici.hwnd);
                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }

            // If the command cannot be identified through the verb string, then 
            // check the identifier offset.
            else
            {
                // Is the command identifier offset supported by this context menu 
                // extension?
                if (NativeMethods.LowWord(ici.lpVerb.ToInt32()) == IDM_DISPLAY)
                {
                    OnVerbDisplayFileName(ici.hwnd);
                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }
            */
			
        }

        /// <summary>
        /// Get information about a shortcut menu command, including the help string 
        /// and the language-independent, or canonical, name for the command.
        /// </summary>
        /// <param name="idCmd">Menu command identifier offset.</param>
        /// <param name="uFlags">
        /// Flags specifying the information to return. This parameter can have one 
        /// of the following values: GCS_HELPTEXTA, GCS_HELPTEXTW, GCS_VALIDATEA, 
        /// GCS_VALIDATEW, GCS_VERBA, GCS_VERBW.
        /// </param>
        /// <param name="pReserved">Reserved. Must be IntPtr.Zero</param>
        /// <param name="pszName">
        /// The address of the buffer to receive the null-terminated string being 
        /// retrieved.
        /// </param>
        /// <param name="cchMax">
        /// Size of the buffer, in characters, to receive the null-terminated string.
        /// </param>
        public void GetCommandString(
            UIntPtr idCmd,
            uint uFlags,
            IntPtr pReserved,
            StringBuilder pszName,
            uint cchMax)
        {
            if (idCmd.ToUInt32() == IDM_DISPLAY)
            {
                switch ((GCS)uFlags)
                {
                    case GCS.GCS_VERBW:
                        if (this.verbCanonicalName.Length > cchMax - 1)
                        {
                            Marshal.ThrowExceptionForHR(WinError.STRSAFE_E_INSUFFICIENT_BUFFER);
                        }
                        else
                        {
                            pszName.Clear();
                            pszName.Append(this.verbCanonicalName);
                        }
                        break;

                    case GCS.GCS_HELPTEXTW:
                        if (this.verbHelpText.Length > cchMax - 1)
                        {
                            Marshal.ThrowExceptionForHR(WinError.STRSAFE_E_INSUFFICIENT_BUFFER);
                        }
                        else
                        {
                            pszName.Clear();
                            pszName.Append(this.verbHelpText);
                        }
                        break;
                }
            }
        }

        #endregion
    }
	
}