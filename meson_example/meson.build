project('meson_example', 'cpp')

src = ['main.cpp']


if get_option('combo_opt') == 'one'
    defines = ['-DONE']
elif get_option('combo_opt') == 'two'
    defines = ['-DTWO']
else
    defines = ['-DTHREE']
endif

executable('test_exe',
    sources : src,
    cpp_args : defines
)