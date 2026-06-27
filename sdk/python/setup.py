from setuptools import setup, find_packages

setup(
    name="rtm-sdk",
    version="1.0.0",
    description="Official Python SDK for Raptoreum (RTM) JSON-RPC API",
    long_description=open("README.md").read(),
    long_description_content_type="text/markdown",
    author="Raptoreum Developers",
    author_email="dev@raptoreum.com",
    url="https://github.com/Raptor3um/rtm-sdk",
    py_modules=["raptoreum"],
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
    python_requires=">=3.6",
)
