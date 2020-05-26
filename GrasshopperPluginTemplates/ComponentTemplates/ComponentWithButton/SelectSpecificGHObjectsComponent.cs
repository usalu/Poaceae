using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Undo.Actions;
using Rhino.Geometry;

namespace Salt.GH_MIDI.Components
{
    /// <summary>
    /// Select with the button excatly one type of IGH_DocumentObject like GH_NumberSlider.
    /// It is it not possible to select the same object by more than one component.
    /// All synchronized objects will be stored statically.
    /// When the component and the selected objects get copied, it will automatically select the newly created objects.
    /// It keeps track of the last 128 deleted objects and if one gets recreated (e.g. with control+z),
    /// it will automatically add the object again to the selection.
    /// </summary>
    public abstract class SelectSpecificGHObjectsComponent : ButtonComponent
    {
        protected bool isSerialized;
        protected bool isInitialized;
        protected GH_Point2D[] serializedDocumentObjectPivotVectors = new GH_Point2D[0];

        /// <summary>
        /// Keeps track of the last 128 deleted objects.
        /// </summary>
        private Guid[] _deletedDocumentObjectsGuid = new Guid[128];
        private int _deletedDocumentObjectCounter;

        public static List<IGH_DocumentObject> synchronizedDocumentObjects = new List<IGH_DocumentObject>();
        public IGH_DocumentObject[] selectedDocumentObjects = new IGH_DocumentObject[0];


        /// <summary>
        ///     Remove all the documentObjects from the synchronization list if they exist.
        /// </summary>
        protected static void RemoveSynchronizedDocumentObjects(IGH_DocumentObject[] documentObjects)
        {
            foreach (var documentObject in documentObjects)
                if (synchronizedDocumentObjects.Contains(documentObject))
                    synchronizedDocumentObjects.Remove(documentObject);
        }

        /// <summary>
        ///     Add all the documentObjects to the synchronization list if they don't already exist.
        /// </summary>
        protected static void AddSynchronizedDocumentObjects(IGH_DocumentObject[] documentObjects)
        {
            foreach (var documentObject in documentObjects)
                if (!synchronizedDocumentObjects.Contains(documentObject))
                    synchronizedDocumentObjects.Add(documentObject);
        }

        /// <summary>
        /// Initializes a new instance of the SelectGHObjectsComponent class.
        /// </summary>
        protected SelectSpecificGHObjectsComponent(
                string name,
                string nickname,
                string description,
                string category,
                string subCategory,
                string buttonName)
            : base(name, nickname, description, category, subCategory,buttonName)
        {
        }

        /// <summary>
        ///     This makes sure to subscribe to the buttonClickEvent from the abstract class which gets triggered by the custom
        ///     attribute class.
        /// </summary>
        /// <param name="document"></param>
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            ButtonClickEvent += OnButtonClicked;
        }

        /// <summary>
        ///     Make sure that all selected documentObjects are removed from static documentObject list and they can be selected again.
        /// </summary>
        public override void RemovedFromDocument(GH_Document document)
        {
            RemoveSynchronizedDocumentObjects(selectedDocumentObjects);
            ButtonClickEvent -= OnButtonClicked;
            document.ObjectsDeleted -= OnObjectsDeleted;
            base.RemovedFromDocument(document);
        }

        /// <summary>
        ///     Update the selected and the synchronizing documentObjects by removing the old documentObjects and replace them with a set of new
        ///     documentObjects.
        /// </summary>
        /// <param name="newSelectedDocumentObjects">New documentObjects to synchronize</param>
        protected virtual void UpdateDocumentObjects(IGH_DocumentObject[] newSelectedDocumentObjects)
        {
            RemoveSynchronizedDocumentObjects(selectedDocumentObjects);
            selectedDocumentObjects = newSelectedDocumentObjects;
            AddSynchronizedDocumentObjects(selectedDocumentObjects);
            ExpireSolution(true);
        }
        
        /// <summary>
        ///     Check if a documentObject from the selected documentObjects got deleted and update the documentObjects if so.
        /// </summary>
        protected virtual void OnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            var intersectingDocumentObjects = selectedDocumentObjects.Intersect(e.Objects).ToArray();
            if (intersectingDocumentObjects.Any())
            {
                foreach (var documentObjectGuid in intersectingDocumentObjects.Select(x => x.InstanceGuid))
                {
                    _deletedDocumentObjectsGuid[_deletedDocumentObjectCounter % 128] = documentObjectGuid;
                    _deletedDocumentObjectCounter++;
                }
                UpdateDocumentObjects(selectedDocumentObjects.Except(intersectingDocumentObjects).ToArray());
            }
        }

        /// <summary>
        ///     Check if a deleted object got added again and add it again to selecting in case.
        /// </summary>
        protected virtual void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            var intersectingDocumentObjectsGuid = _deletedDocumentObjectsGuid.Intersect(e.Objects.Select(x => x.InstanceGuid)).ToArray();
            if (intersectingDocumentObjectsGuid.Any())
            {
                _deletedDocumentObjectCounter -= intersectingDocumentObjectsGuid.Length;
                UpdateDocumentObjects(selectedDocumentObjects.Concat(e.Objects.Where(x => intersectingDocumentObjectsGuid.Contains(x.InstanceGuid))).ToArray());
            }
        }

        /// <summary>
        ///     Select new documentObjects and update them if they are not already selected by another component.
        /// </summary>
        protected virtual void OnButtonClicked(object sender, EventArgs e)
        {
            var newSelectedDocumentObjects = GetSelectedObjects();
            if (newSelectedDocumentObjects.Except(selectedDocumentObjects).Intersect(synchronizedDocumentObjects).Any())
            {
                ClearRuntimeMessages();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Some objects are already selected by another component. Please select again.");
                return;
            }
            UpdateDocumentObjects(newSelectedDocumentObjects);
        }

        /// <summary>
        /// By default all selected objects get added after the button click. If you only want a specific type add "&& x is YourSpecificGHType" next to "x is IGH_ActiveObject". It needs to be obviously an IGH_DocumentObject derived type.
        /// </summary>
        /// <returns>The selected objects when the button was clicked.</returns>
        protected virtual IGH_DocumentObject[] GetSelectedObjects()
        {
            return OnPingDocument().SelectedObjects().Where(x => x is IGH_ActiveObject).ToArray();
        }

        /// <summary>
        ///     Serialization works over vectors instead of Guids. Because if documentObjects and the component are copied together the
        ///     instanceGuid will be newly assigned and the new documentObjects will not be automatically selected.
        ///     As the distances are preserved when copying this offers a good alternative.
        /// </summary>
        public override bool Write(GH_IWriter writer)
        {
            serializedDocumentObjectPivotVectors = selectedDocumentObjects.Select(x =>
                new GH_Point2D(Math.Round(Math.Abs(Attributes.Pivot.X - x.Attributes.Pivot.X), 4),
                    Math.Round(Math.Abs(Attributes.Pivot.Y - x.Attributes.Pivot.Y), 4))).ToArray();
            var chunk = writer.CreateChunk("SelectedDocumentObjects");
            for (var i = 0; i < selectedDocumentObjects.Length; i++)
                chunk.SetPoint2D($"DocumentObject{i}", serializedDocumentObjectPivotVectors[i]);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            serializedDocumentObjectPivotVectors = reader.FindChunk("SelectedDocumentObjects").Items.Select(x => x.InternalData)
                .Cast<GH_Point2D>().ToArray();
            isSerialized = false;
            return base.Read(reader);
        }

        /// <summary>
        ///     Check if the vectors of the other components fit to the serialized vectors and the type fits.
        ///     If that is the case they will be added to the selected documentObjects.
        ///     In case that there are multiple documentObjects with the same coordinate it will pick the first ones until it reaches the original count.
        /// </summary>
        public void DeserializeDocumentObjects()
        {
            var ghDocument = Instances.ActiveCanvas.Document ?? OnPingDocument();
            var serializedDocumentObjects = ghDocument.Objects.Except(synchronizedDocumentObjects).Where(x => x is IGH_ActiveObject && serializedDocumentObjectPivotVectors.Contains(new GH_Point2D(
                                                                                                                  Math.Round(Math.Abs(Attributes.Pivot.X - x.Attributes.Pivot.X), 4),
                                                                                                                  Math.Round(Math.Abs(Attributes.Pivot.Y - x.Attributes.Pivot.Y), 4))))
                .Reverse().Take(serializedDocumentObjectPivotVectors.Length).ToArray();
            if (serializedDocumentObjects.Any())
                UpdateDocumentObjects(serializedDocumentObjects);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            /*
             The object deleted event has to be subscribed here because if subscribed in AddedToDocument (where it belongs to)
             then it will for some reason not subscribe to the event if the component gets copy and pasted. 
             It will only run exactly once on creation time of the component.
             */
            if (!isInitialized)
            {
                OnPingDocument().ObjectsDeleted += OnObjectsDeleted;
                OnPingDocument().ObjectsAdded += OnObjectsAdded;
                isInitialized = true;
            }

            /*The deserialization can only happen when all the components are loaded.
             Therefore the only way to make sure this is the case, it has to run in the instance solver.
             This is the only method that is triggered after the all the components are already loaded.
            */
            if (!isSerialized)
            {
                DeserializeDocumentObjects();
                isSerialized = true;
            }
        }
    }
}