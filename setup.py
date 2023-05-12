# Copyright (C) 2023 Mark Mc Mahon and Contributors
# https://github.com/pywinauto/injected/graphs/contributors
# All rights reserved.
#
# Redistribution and use in source and binary forms, with or without
# modification, are permitted provided that the following conditions are met:
#
# * Redistributions of source code must retain the above copyright notice, this
#   list of conditions and the following disclaimer.
#
# * Redistributions in binary form must reproduce the above copyright notice,
#   this list of conditions and the following disclaimer in the documentation
#   and/or other materials provided with the distribution.
#
# * Neither the name of pywinauto nor the names of its
#   contributors may be used to endorse or promote products derived from
#   this software without specific prior written permission.
#
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
# AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
# IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
# DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
# FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
# DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
# SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
# CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
# OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
# OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

"""Install and build injectdll distributions"""

import os
import shutil
import subprocess
from setuptools import setup
from setuptools.dist import Distribution
from injectdll import sysinfo

cmake_lists = ['./backends/dotnet/CMakeLists.txt', './backends/hook/CMakeLists.txt']

dll_directories = ['./backends/dotnet/', './backends/hook/']

package_dll_directories = ['./injectdll/backends/dotnet/bin/' + sysinfo.os_arch(), './injectdll/backends/hook/bin/' + sysinfo.os_arch()]

for cmake_list in cmake_lists:
    build_dir = os.path.dirname(cmake_list) + '/build'

    os.makedirs(build_dir, exist_ok=True)

    subprocess.check_call(['cmake', '-B ' + build_dir, '-S ' + os.path.dirname(cmake_list)])
    subprocess.check_call(['cmake', '--build', build_dir])

for dll_directory, package_dll_directory in zip(dll_directories, package_dll_directories):
    for root, dirs, files in os.walk(dll_directory):
        for file in files:
            if file.endswith('.dll'):
                src_file_path = os.path.join(root, file)
                dest_file_path = os.path.join(package_dll_directory, file)
                os.makedirs(os.path.dirname(dest_file_path), exist_ok=True)
                shutil.copy(src_file_path, dest_file_path)


class BinaryDistribution(Distribution):
    def is_pure(self):
        return False


setup(name='injectdll',
      version='0.0.1',
      description='A set of Python modules to inject DLls into applications for the Microsoft Windows',
      keywords="windows gui .net inject testing test desktop dll wpf",
      url="https://github.com/pywinauto/injected",
      author='Mark Mc Mahon and Contributors',
      author_email='pywinauto@yandex.ru',
      long_description="""
It allows to inject DLls into applications for the Microsoft Windows.
""",
      platforms=['win32'],

      packages=["injectdll"],
      include_package_data=True,

      license="BSD 3-clause",
      classifiers=[
          'Development Status :: 5 - Production/Stable',
          'Environment :: Console',
          'Intended Audience :: Developers',
          'License :: OSI Approved :: BSD License',
          'Operating System :: Microsoft :: Windows',
          'Programming Language :: Python',
          'Programming Language :: Python :: 3.7',
          'Programming Language :: Python :: 3.8',
          'Programming Language :: Python :: 3.9',
          'Programming Language :: Python :: 3.10',
          'Programming Language :: Python :: 3.11',
          'Programming Language :: Python :: Implementation :: CPython',
          'Topic :: Software Development :: Libraries :: Python Modules',
          'Topic :: Software Development :: Testing',
          'Topic :: Software Development :: User Interfaces'
      ],
      install_requires=['six', 'pywin32'],
      python_requires='>=3.7',
      )
