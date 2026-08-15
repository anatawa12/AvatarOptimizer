find . -type f -name '*.cs' -print0 \
  | xargs -0 awk '
    {
      # remove comments
      sub(/\/\/.*/, "", $0)
    };
    /^[[:space:]]*#[[:space:]]*/{
      # remove leading "#"
      sub(/^[[:space:]]*#[[:space:]]*/, "", $0)
      # split extract preprocessor operation name
      op=$1; $1=""; gsub(/[[:space:]]/, "", $0)
      if (op == "if") {
        conditions = conditions " " $0
        print conditions
      } else if (op == "elif") {
        sub(/ [^[:space:]]*$/, " " $0, conditions)
        print conditions
      } else if (op == "else") {
      } else if (op == "endif") {
        sub(/ [^[:space:]]*$/, "", conditions)
        print conditions
      }
    }
  ' \
  | sort | uniq
