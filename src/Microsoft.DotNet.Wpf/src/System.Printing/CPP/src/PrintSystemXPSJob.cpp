// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Xml;
using namespace System::Xml::XPath;

using namespace System::Runtime::Serialization;
using namespace System::Runtime::Serialization::Formatters;
using namespace System::Runtime::Serialization::Formatters::Binary;

using namespace System::Printing::Interop;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;
using namespace System::IO::Packaging;
using namespace System::Windows::Xps::Packaging;


#ifndef  __PRINTSYSTEMXPSJOB_HPP__
#include <PrintSystemXpsJob.hpp>
#endif


#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

#ifndef  __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#include <ObjectsAttributesValuesFactory.hpp>
#endif

#ifndef __PRINTSYSTEMJOBENUMS_HPP__
#include <PrintSystemJobEnums.hpp>
#endif


using namespace System::Printing::Activation;

PrintSystemXpsJob::
PrintSystemXpsJob(
    PrintSystemJobInfo^     jobInfo
    ):
PrintSystemJob(jobInfo)
{
    try
    {
        Initialize();
        //
        // Gather out the name of all the properties within the object
        //
        array<String^>^ propertiesAsStrings = PrintSystemXpsJob::GetAllPropertiesFilter();

        PrintQueueStream^ printStream = gcnew PrintQueueStream(JobInfo->HostingPrintQueue);

        JobInfo->JobIdentifier = printStream->JobIdentifier;

        System::IO::Packaging::Package^ package = Package::Open(printStream,
                                                                FileMode::Create,
                                                                FileAccess::ReadWrite);

        #pragma warning ( disable:4691 )
        metroPackage = gcnew System::Windows::Xps::Packaging::XpsDocument(package);
        #pragma warning ( default:4691 )

        //
        // Needs to be hooked for the ReportJobProgress DCR
        //
        //metroPackage->PackagingProgressEvent += gcnew System::Windows::Xps::Packaging::PackagingProgressEventHandler(this, &PrintSystemXpsJob::HandlePackagingProgressEvent);
    }
    catch (PrintSystemException^ exception)
    {
        throw exception;
    }
    catch (SystemException^ internalException)
    {
        throw gcnew PrintJobException("PrintSystemException.PrintSystemXpsJob.Create",
                                      internalException);
    }
    __finally
    {
    }
}


void
PrintSystemXpsJob::
InternalDispose(
    bool disposing
    )
{
    if(!IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (disposing)
                {
                    if (metroPackage)
                    {
                        delete metroPackage;
                    }
                }
            }
            __finally
            {
                __try
                {
                    PrintSystemObject::InternalDispose(disposing);
                }
                __finally
                {
                    this->IsDisposed = true;
                    System::Threading::Monitor::Exit(this);
                }
            }
        }
    }
}

void
PrintSystemXpsJob::XpsDocument::
set(
    System::Windows::Xps::Packaging::XpsDocument^   reachPackage
    )
{
    metroPackage = reachPackage;
}

System::Windows::Xps::Packaging::XpsDocument^
PrintSystemXpsJob::XpsDocument::
get(
    void
    )
{
    return metroPackage;
}

void
PrintSystemXpsJob::Name::
set(
    String^   name
    )
{
    if(PrintSystemObject::Name::get() != name ||
       (name&&
       !name->Equals(PrintSystemObject::Name::get())))
    {
        PrintSystemObject::Name::set(name);

        PropertiesCollection->GetProperty("Name")->Value = name;
    }
}

String^
PrintSystemXpsJob::Name::
get(
    void
    )
{
    return PrintSystemObject::Name::get();

}



void
PrintSystemXpsJob::
Commit(
    void
    )
{
   if (!metroPackage)
   {
       throw gcnew PrintJobException("PrintSystemException.PrintSystemXpsJob.Commited");
   }
   else
    {
        try
        {

        }
        __finally
        {

        }
    }
}

void
PrintSystemXpsJob::
Refresh(
    void
    )
{
}

void
PrintSystemXpsJob::
Initialize(
    void
    )
{
    //
    // Override the set_Name property in the base class
    //
    ((PrintStringProperty^)PropertiesCollection->GetProperty("Name"))->
    ChangeHandler = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintSystemXpsJob::Name::set);

    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    for(Int32 numOfPrimaryAttributes=0;
        numOfPrimaryAttributes < PrintSystemXpsJob::primaryAttributeNames->Length;
        numOfPrimaryAttributes++)
    {
        PrintProperty^ printSystemAttributeValue = nullptr;

        printSystemAttributeValue =
        ObjectsAttributesValuesFactory::Value->Create(this->GetType(),
                                                      primaryAttributeNames[numOfPrimaryAttributes],
                                                      propertiesDelegates[numOfPrimaryAttributes]);

        PrintSystemObject::PropertiesCollection->Add(printSystemAttributeValue);
    }
}


array<MulticastDelegate^>^
PrintSystemXpsJob::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(primaryAttributeNames->Length);
    return propertiesDelegates;
}

array<String^>^
PrintSystemXpsJob::
GetAllPropertiesFilter(
    void
    )
{
    //
    // Properties = Base Class Properties + Inherited Class Properties
    //
    array<String^>^ allPropertiesFilter = gcnew array<String^>(PrintSystemObject::BaseAttributeNames()->Length +
                                                               PrintSystemXpsJob::primaryAttributeNames->Length);

    //
    // First Add the Base Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemObject::BaseAttributeNames()->Length;
        numOfAttributes++)
    {
        allPropertiesFilter[numOfAttributes] = PrintSystemObject::BaseAttributeNames()[numOfAttributes];
    }

    //
    // Then Add the Inherited Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemXpsJob::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        allPropertiesFilter[PrintSystemObject::BaseAttributeNames()->Length + numOfAttributes] =
        PrintSystemXpsJob::primaryAttributeNames[numOfAttributes];
    }

    return allPropertiesFilter;
}

void
PrintSystemXpsJob::
RegisterAttributesNamesTypes(
    void
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintSystemObject::RegisterAttributesNamesTypes(PrintSystemXpsJob::attributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemXpsJob::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintSystemXpsJob::primaryAttributeNames[numOfAttributes],
                                PrintSystemXpsJob::primaryAttributeTypes[numOfAttributes]);
    }
}

PrintProperty^
PrintSystemXpsJob::
CreateAttributeNoValue(
    String^ attributeName
    )
{
    Type^ type = (Type^)PrintSystemXpsJob::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName);
}

PrintProperty^
PrintSystemXpsJob::
CreateAttributeValue(
    String^ attributeName,
    Object^ attributeValue
    )
{
    Type^ type = (Type^)PrintSystemXpsJob::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue);
}

PrintProperty^
PrintSystemXpsJob::
CreateAttributeNoValueLinked(
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintSystemXpsJob::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,delegate);
}

PrintProperty^
PrintSystemXpsJob::
CreateAttributeValueLinked(
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintSystemXpsJob::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue,delegate);
}

PrintPropertyDictionary^
PrintSystemXpsJob::
get_InternalPropertiesCollection(
    String^ attributeName
    )
{
    return nullptr;
}

void
PrintSystemXpsJob::
HandlePackagingProgressEvent(
    Object^                     sender,
    PackagingProgressEventArgs^ e
    )
{
    JobInfo->ReportProgress(JobOperation::JobProduction, e->Action, nullptr);
}

