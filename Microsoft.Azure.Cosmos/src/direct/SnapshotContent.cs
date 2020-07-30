//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// SnapshotContent.
    /// </summary>

    internal sealed class SnapshotContent : JsonSerializable
    {
        private IList<string> partitionKeyRangeResourceIds;
        private IList<string> dataDirectories;
        private IList<string> partitionKeyRanges;
        private IList<PartitionKeyRange> partitionKeyRangeList;
        private DocumentCollection collectionResource;
        private Database databaseResource;

        [JsonConstructor]
        public SnapshotContent()
        {

        }

        // To instantiate snapshot of provisioned throughput collection
        internal SnapshotContent(
            string serializedCollection, 
            string serializedDatabase, 
            string serializedOffer, 
            IList<string> serializedPkranges, 
            OperationType operationType)
        {
            this.ArgumentStringNotNullOrWhiteSpace(serializedCollection, nameof(serializedCollection));
            this.ArgumentStringNotNullOrWhiteSpace(serializedDatabase, nameof(serializedDatabase));
            this.ArgumentStringNotNullOrWhiteSpace(serializedOffer, nameof(serializedOffer));
            if (serializedPkranges == null || serializedPkranges.Count == 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(serializedPkranges)));
            }

            if (operationType == OperationType.Invalid)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(operationType)));
            }
           
            this.SerializedCollection = serializedCollection;
            this.SerializedDatabase = serializedDatabase;
            this.SerializedOffer = serializedOffer;
            this.SerializedPartitionKeyRanges = serializedPkranges;
            this.OperationType = operationType;
        }

        // To instantiate snapshot of a non-shared ru database
        internal SnapshotContent(
            string serializedDatabase,
            OperationType operationType)
        {
            this.ArgumentStringNotNullOrWhiteSpace(serializedDatabase, nameof(serializedDatabase));
            if (operationType == OperationType.Invalid)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(operationType)));
            }

            this.SerializedDatabase = serializedDatabase;
            this.OperationType = operationType;
        }

        // To instantiate snapshot of a shared ru database
        internal SnapshotContent(
            string serializedDatabase,
            string serializedOffer,
            NameValueCollection geoLinkToPKRangeRIds,
            OperationType operationType)
        {
            this.ArgumentStringNotNullOrWhiteSpace(serializedDatabase, nameof(serializedDatabase));
            this.ArgumentStringNotNullOrWhiteSpace(serializedOffer, nameof(serializedOffer));
            if (operationType == OperationType.Invalid)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(operationType)));
            }

            // TODO: Kalyan - Add the validation and store geoLinkToPKRangeIds.
            this.SerializedDatabase = serializedDatabase;
            this.OperationType = operationType;
            this.SerializedOffer = serializedOffer;
        }

        // To instantiate snapshot of a shared ru collection
        internal SnapshotContent(
            string serializedDatabase,
            string serializedCollection,
            IList<string> serializedPkranges,
            NameValueCollection geoLinkToPKRangeRIds,
            OperationType operationType)
        {
            this.ArgumentStringNotNullOrWhiteSpace(serializedDatabase, nameof(serializedDatabase));
            this.ArgumentStringNotNullOrWhiteSpace(serializedCollection, nameof(serializedCollection));
            if (serializedPkranges == null || serializedPkranges.Count == 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(serializedPkranges)));
            }

            if (operationType == OperationType.Invalid)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(operationType)));
            }

            // TODO: Kalyan - Add the validation and store geoLinkToPKRangeIds.
            this.SerializedDatabase = serializedDatabase;
            this.OperationType = operationType;
            this.SerializedCollection = serializedCollection;
        }

        [JsonIgnore]
        public IList<PartitionKeyRange> PartitionKeyRangeList
        {
            get
            {
                if (this.partitionKeyRangeList == null && this.SerializedPartitionKeyRanges != null)
                {
                    this.partitionKeyRangeList = new List<PartitionKeyRange>();
                    foreach (string partitionKeyRange in this.SerializedPartitionKeyRanges)
                    {
                        this.partitionKeyRangeList.Add(this.GetResourceIfDeserialized<PartitionKeyRange>(partitionKeyRange));
                    }
                }

                return this.partitionKeyRangeList;
            }
        }

        [JsonIgnore]
        public DocumentCollection DocumentCollection
        {
            get
            {
                if (this.collectionResource == null && this.SerializedCollection != null)
                {
                    this.collectionResource = this.GetResourceIfDeserialized<DocumentCollection>(this.SerializedCollection);
                }

                return this.collectionResource;
            }
        }

        [JsonIgnore]
        public Database Database
        {
            get
            {
                if (this.databaseResource == null && this.SerializedDatabase != null)
                {
                    this.databaseResource = this.GetResourceIfDeserialized<Database>(this.SerializedDatabase);
                }

                return this.databaseResource;
            }
        }

        /// <summary>
        /// Gets the list of PartitionKeyRangeRids.
        /// </summary>
        [JsonProperty(PropertyName = Constants.SnapshotProperties.PartitionKeyRangeResourceIds)]
        public IList<string> PartitionKeyRangeResourceIds
        {
            get
            {
                if (this.partitionKeyRangeResourceIds == null)
                {
                    this.partitionKeyRangeResourceIds = base.GetValue<IList<string>>(Constants.SnapshotProperties.PartitionKeyRangeResourceIds);
                }

                return this.partitionKeyRangeResourceIds;
            }
            internal set
            {
                this.partitionKeyRangeResourceIds = value;
            }
        }

        /// <summary>
        /// Gets the list of Data Directories.
        /// </summary>
        [JsonProperty(PropertyName = Constants.SnapshotProperties.DataDirectories)]
        public IList<string> DataDirectories
        {
            get
            {
                if (this.dataDirectories == null)
                {
                    this.dataDirectories = base.GetValue<IList<string>>(Constants.SnapshotProperties.DataDirectories);
                }

                return this.dataDirectories;
            }
            internal set
            {
                this.dataDirectories = value;
            }
        }

        /// <summary>
        /// Gets the Metadata Directory.
        /// </summary>
        [JsonProperty(PropertyName = Constants.SnapshotProperties.MetadataDirectory)]
        public string MetadataDirectory
        {
            get
            {
                return base.GetValue<string>(Constants.SnapshotProperties.MetadataDirectory);
            }
            internal set
            {
                base.SetValue(Constants.SnapshotProperties.MetadataDirectory, value);
            }
        }

        [JsonProperty(PropertyName = Constants.SnapshotProperties.OperationType)]
        public OperationType OperationType
        {
            get
            {
                string strValue = base.GetValue<string>(Constants.Properties.OperationType);
                if (string.IsNullOrEmpty(strValue))
                {
                    return OperationType.Invalid;
                }

                OperationType type = (OperationType)Enum.Parse(typeof(OperationType), strValue, true);
                return type;
            }
            internal set
            {
                base.SetValue(Constants.Properties.OperationType, value.ToString());
            }
        }

        [JsonProperty(PropertyName = Constants.SnapshotProperties.DatabaseContent)]
        public string SerializedDatabase
        {
            get
            {
                return base.GetValue<string>(Constants.SnapshotProperties.DatabaseContent);
            }
            internal set
            {
                base.SetValue(Constants.SnapshotProperties.DatabaseContent, value);
            }
        }

        [JsonProperty(PropertyName = Constants.SnapshotProperties.OfferContent)]
        public string SerializedOffer
        {
            get
            {
                return base.GetValue<string>(Constants.SnapshotProperties.OfferContent);
            }
            internal set
            {
                base.SetValue(Constants.SnapshotProperties.OfferContent, value);
            }
        }

        [JsonProperty(PropertyName = Constants.SnapshotProperties.CollectionContent)]
        public string SerializedCollection
        {
            get
            {
                return base.GetValue<string>(Constants.SnapshotProperties.CollectionContent);
            }
            internal set
            {
                base.SetValue(Constants.SnapshotProperties.CollectionContent, value);
            }
        }

        /// <summary>
        /// Gets the list of PartitionKeyRanges.
        /// </summary>
        [JsonProperty(PropertyName = Constants.SnapshotProperties.PartitionKeyRanges)]
        public IList<string> SerializedPartitionKeyRanges
        {
            get
            {
                if (this.partitionKeyRanges == null)
                {
                    this.partitionKeyRanges = base.GetValue<IList<string>>(Constants.SnapshotProperties.PartitionKeyRanges);
                }

                return this.partitionKeyRanges;
            }
            internal set
            {
                base.SetValue(Constants.SnapshotProperties.PartitionKeyRanges, value);
            }
        }

        internal override void OnSave()
        {
            base.OnSave();

            // SerializedPartitionKeyRanges, SerializedCollection, SerializedOffer, SerializedDatabase and OperationType properties are generated by BE when creating the system snapshot.
            // Hence these properties do not need a public setter for it and dont need to set explicitly in the OnSave (since they will already be part of the propertybag)
            if (this.partitionKeyRangeResourceIds != null)
            {
                base.SetValue(Constants.SnapshotProperties.PartitionKeyRangeResourceIds, this.partitionKeyRangeResourceIds);
            }

            if (this.dataDirectories != null)
            {
                base.SetValue(Constants.SnapshotProperties.DataDirectories, this.dataDirectories);
            }
        }

        private T GetResourceIfDeserialized<T>(string body) where T : Resource, new()
        {
            try
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(body);
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    ITypeResolver<T> typeResolver = JsonSerializable.GetTypeResolver<T>();
                    return JsonSerializable.LoadFrom<T>(stream, typeResolver);
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal override void Validate()
        {
            base.Validate();
            base.GetValue<IList<string>>(Constants.SnapshotProperties.PartitionKeyRangeResourceIds);
            base.GetValue<IList<string>>(Constants.SnapshotProperties.DataDirectories);
        }

        private void ArgumentStringNotNullOrWhiteSpace(string parameter, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, nameof(parameterName)));
            }

            if (string.IsNullOrWhiteSpace(parameter))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, parameterName));
            }
        }
    }
}
