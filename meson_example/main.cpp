#include <iostream>

int main()
{
#if defined(ONE)
    std::string value = "one";
#elif defined(TWO)
    std::string value = "two";
#else
    std::string value = "three";
#endif
    std::cout << "Hello world! " << value << std::endl;
    return 0;
}