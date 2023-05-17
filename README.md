# injectdll

injectdll is a Python package that allows you to inject DLLs into running processes on Windows (32 or 64-bit). 
This can be useful for various tasks, such as debugging or adding functionality to existing applications.

It contains test DLLs for classic Win32 API and WPF applications.

Link to PyPi: https://pypi.org/project/injectdll/

### How to use

    python.exe example_injector.py \<Process name\> \<path to dll\> 

### Development

1. Clone repository
       
        https://github.com/pywinauto/injected.git

2. Create new features or improve exiting (for the injectdll package)

3. After work run setup.py from project root to check standalone

        python.exe setup.py bdist_wheel

### Requirements

#### For use as package

* Python >= 3.7
* pywin32 >= 306
* six >= 1.16.0

#### For development purpose

* setuptools >= 65.5.1

* wheel >= 0.38.4

* .NET Framework Targeting Pack >= 4.8

* .NET Compiler Platform

* MSBuild

* MSVC >= 143

* cmake >= 3.26.3


**NOTE**: You may use PyCharm + Visual Studio with C++/C# components for development 

### License

This software is open-source under the BSD 3-Clause License.

See the LICENSE file in this repository.

Dependencies are licensed by their own licenses.