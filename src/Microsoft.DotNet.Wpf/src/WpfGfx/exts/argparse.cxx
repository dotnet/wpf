// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "precomp.hxx"

CommandLine::CommandLine()
{
    m_pArguments = NULL;
    m_cCount = 0;
}

CommandLine::~CommandLine()
{
    for (UINT i = 0; i < m_cCount; i++)
    {
        delete m_pArguments[i].string;
    }

    if (m_pArguments)
    {
        delete m_pArguments;
        m_pArguments = NULL;
    }
}

size_t GetNextWordStart(size_t c, size_t cChars, PCSTR str)
{
    while (c < cChars && isspace(str[c]))
    {
        c++;
    }

    return c;
}

// Returns a position one past the end of the next word.
size_t GetNextWordEnd(size_t c, size_t cChars, PCSTR str)
{
    c = GetNextWordStart(c, cChars, str);
    
    while (c < cChars && !isspace(str[c]))
    {
        c++;
    }

    return c;
}


HRESULT
CommandLine::CreateFromString(OutputControl& , PCSTR args, __deref_out CommandLine** ppCommandLine)
{
    HRESULT hr = S_OK;
    CommandLine* pCommandLine = NULL;
    
    size_t cChars = 0;

    hr = StringCchLengthA(args, STRSAFE_MAX_CCH, &cChars);

    if (SUCCEEDED(hr))
    {
        pCommandLine = new CommandLine();

        if (!pCommandLine)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            size_t c = 0;
            while (c < cChars)
            {
                size_t uNextWordStart = GetNextWordStart(c, cChars, args);
                size_t uNextWordEnd = GetNextWordEnd(uNextWordStart, cChars, args);

                if (uNextWordStart != uNextWordEnd)
                {
                    pCommandLine->m_cCount++;
                }

                c = uNextWordEnd;
            }

            pCommandLine->m_pArguments = new CommandLine::ARGUMENT[pCommandLine->m_cCount];

            if (!pCommandLine->m_pArguments)
            {
                delete pCommandLine;
                pCommandLine = NULL;
                hr = E_OUTOFMEMORY;
            }
            else
            {
                c = 0;
                size_t uArg = 0;
                while (c < cChars)
                {
                    size_t uNextWordStart = GetNextWordStart(c, cChars, args);
                    size_t uNextWordEnd = GetNextWordEnd(uNextWordStart, cChars, args);

                    if (uNextWordStart != uNextWordEnd)
                    {
                        bool fIsOption = false;

                        if (args[uNextWordStart] == '-' || args[uNextWordStart] == '/')
                        {
                            fIsOption = true;
                            uNextWordStart++;
                        }

                        pCommandLine->m_pArguments[uArg].fIsOption = fIsOption;
                        pCommandLine->m_pArguments[uArg].cchLength = uNextWordEnd - uNextWordStart;
                        pCommandLine->m_pArguments[uArg].string = new char[pCommandLine->m_pArguments[uArg].cchLength + 1];

                        if (pCommandLine->m_pArguments[uArg].string)
                        {
                            StringCchCopyA(pCommandLine->m_pArguments[uArg].string, pCommandLine->m_pArguments[uArg].cchLength + 1, &args[uNextWordStart]);
                        }

                        uArg++;
                    }

                    c = uNextWordEnd;
                }
            }
        }
    }

    *ppCommandLine = pCommandLine;

    return hr;
}


