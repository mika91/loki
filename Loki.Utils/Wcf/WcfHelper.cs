using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace Loki.Utils
{
    public static class WcfHelper
    {

        public static String GetServiceContract(object obj)
        {
            // Try to find a parent class/interface with ServiceContrat Attribute
            var type = obj.GetType();
            var sc =
                type.GetParentTypes()
                    .SelectMany( t => Attribute.GetCustomAttributes(t).Select(a => new{ Type = t, ServiceContract = a as ServiceContractAttribute}))
                    .FirstOrDefault( x => x.ServiceContract != null);

            // No Service contract found
            if (sc == null)
            {
                // TODO
                return null;
            }

            // Return ServiceContract name
            return String.IsNullOrEmpty(sc.ServiceContract.ConfigurationName)
                 ? sc.Type.FullName
                 : sc.ServiceContract.ConfigurationName;
        }


        #region Resolve Endpoints/Bindings/Behaviors from configuration file

        /// <summary>
        /// Get ServiceModel section from configuration file
        /// </summary>
        /// <param name="exepath">Path to the configuration file</param>
        /// <returns>The ServiceModel read from configuration file, or null if not found</returns>
        private static ServiceModelSectionGroup GetServiceSection(string exepath = null)
        {
            Configuration config = (exepath == null)
               ? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
               : ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = exepath }, ConfigurationUserLevel.None);

            return ServiceModelSectionGroup.GetSectionGroup(config);
        }

        /// <summary>
        /// Get binding from configuration file
        /// </summary>
        /// <param name="name">Binding name</param>
        /// <param name="exepath">Path of the configuration file</param>
        /// <returns>Binding read from configuration file, or null if not found</returns>
        public static Binding ResolveBinding(string name, string exepath = null)
        {
            // Get Bindings Section
            var serviceModel = GetServiceSection(exepath);
            if (serviceModel == null) return null;

            var section = serviceModel.Bindings;
            if (section == null) return null;

            // Resolve binding according to the specified name
            foreach (var bindingCollection in section.BindingCollections)
            {
                foreach (var conf in bindingCollection.ConfiguredBindings)
                {
                    if ((conf.Name == name)                                         // Named binding 
                     || (conf.Name == "" && bindingCollection.BindingName == name)) // Default binding 
                    {
                        var bindingElement = conf;
                        var binding = (Binding)Activator.CreateInstance(bindingCollection.BindingType);
                        binding.Name = name;
                        bindingElement.ApplyConfiguration(binding);

                        return binding;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get Endpoint behaviors from configuration file
        /// </summary>
        /// <param name="name">Binding name</param>
        /// <param name="exepath">Path of the configuration file</param>
        /// <returns>Endpoint behaviors read from configuration file, or null if not found</returns>
        public static List<IEndpointBehavior> ResolveEndpointBehaviors(string name, string exepath = null)
        {
            // Get Bindings Section
            var serviceModel = GetServiceSection(exepath);
            if (serviceModel == null) return null;

            var section = serviceModel.Behaviors;
            if (section == null) return null;

            // Resolve endpoint behaviors according to the specified name
            var endpointBehaviors = new List<IEndpointBehavior>();

            if (section.EndpointBehaviors.Count > 0 
                && section.EndpointBehaviors[0].Name == name)
            {
                var behaviorCollectionElement = section.EndpointBehaviors[0];

                foreach (BehaviorExtensionElement behaviorExtension in behaviorCollectionElement)
                {
                    object extension = behaviorExtension.GetType().InvokeMember("CreateBehavior",
                          BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                          null, behaviorExtension, null);

                    endpointBehaviors.Add((IEndpointBehavior)extension);
                }

                return endpointBehaviors;
            }

            return null;
        }

        /// <summary>
        /// Get channel configuration from configuration in "app.config/service.model" file.
        /// </summary>
        /// <param name="predicate">Endpoint predicate</param>
        /// /// <param name="exepath">Path of the configuration file</param>
        /// <returns>Endpoint read from configuration file, or null if not found</returns>
        public static ChannelEndpointElement ResolveEndpoint(Func<ChannelEndpointElement, bool> predicate, string exepath = null)
        {
            // Get Bindings Section
            var serviceModel = GetServiceSection(exepath);
            if (serviceModel == null) return null;

            var section = serviceModel.Client;
            if (section == null) return null;

            // Resolve endpoint according to the predicate
            return section.Endpoints.Cast<ChannelEndpointElement>()
                          .FirstOrDefault(predicate);

        }

        #endregion
    }
}
