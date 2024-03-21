function MyFunction (n)
    print ("MyFunction")
    print (n)
end

local function fact(n)
    if n == 0 then
    return 1
    else
    return n * fact(n-1)
    end
end

b = fact(5)
MyFunction(b)

PUB_testvar = 0.5

c = 0.5
d = 'oh harro'